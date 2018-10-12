using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Logging;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Engine.Generation;
using S3Location = Watchman.Engine.Generation.Generic.S3Location;

namespace Watchman.Engine.Tests.Generation
{
    [TestFixture]
    public class CloudFormationAlarmCreatorTests
    {
        public class Resource : IAwsResource
        {
            public string Name { get; set; }
        }

        private Mock<IAmazonCloudFormation> _cloudFormationMock;
        private Mock<IAmazonS3> _s3Mock;

        private void SetupListStacksToReturnStackNames(params string[] stackNames)
        {
            _cloudFormationMock
                .Setup(x => x.ListStacksAsync(It.IsAny<ListStacksRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListStacksResponse
                {
                    StackSummaries = stackNames.Select(s => new StackSummary
                        {
                            StackName = s
                        }).ToList()
                });
        }

        private void SetupCreateStackAsyncToFail()
        {
            _cloudFormationMock
                .Setup(x => x.CreateStackAsync(It.IsAny<CreateStackRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("all gon rong"));
        }

        private void SetStatusForStackName(string stackName, string status)
        {
            SetupStackStatusSequence(stackName, new List<string> { status });
        }

        private void SetupStackStatusSequence(string stackName, IList<string> statuses)
        {
            var i = 0;

            _cloudFormationMock
                .Setup(x => x.DescribeStacksAsync(
                    It.Is<DescribeStacksRequest>(s => s.StackName == stackName),
                    It.IsAny<CancellationToken>())
                )
                .Returns(() =>
                {
                    var result = new DescribeStacksResponse
                    {
                        Stacks = new List<Stack>
                        {
                            new Stack
                            {
                                StackStatus = statuses[i],
                                StackName = stackName
                            }
                        }
                    };

                    i++;
                    return Task.FromResult(result);
                });
        }
        private string ExpectedStackName(AlertingGroupParameters group)
        {
            return $"Watchman-{group.Name.ToLowerInvariant()}";
        }

        private AlertingGroupParameters Group(string name = "group-name", string suffix = "group-suffix", int numberOfStacks = 1)
        {
            return new AlertingGroupParameters(name,
                suffix,
                new List<AlertTarget>()
                {
                    new AlertEmail("test@test.com")
                },
                false,
                numberOfCloudFormationStacks: numberOfStacks
            );
        }


        private static Alarm Alarm(string name = "Test alarm")
        {
            return new Alarm
            {
                AlarmName = name,
                AlarmDefinition = new AlarmDefinition
                {
                    Threshold = new Threshold { ThresholdType = ThresholdType.Absolute },
                    ComparisonOperator = ComparisonOperator.GreaterThanOrEqualToThreshold,
                    DimensionNames = new List<string>(),
                    Statistic = Statistic.Average,
                    Period = TimeSpan.FromMinutes(3)
                },
                Resource = new Resource { Name = $"resource-for-{name}"},
                Dimensions = new List<Dimension>()
            };
        }

        [SetUp]
        public void SetUp()
        {
            _cloudFormationMock = new Mock<IAmazonCloudFormation>();
            _s3Mock = new Mock<IAmazonS3>();
        }

        [Test]
        public async Task SaveChanges_StackExists_StackIsUpdated()
        {
            // arrange
            var alarm = Alarm();
            var group = Group();
            var stackName = ExpectedStackName(group);

            SetupListStacksToReturnStackNames(stackName);
            SetStatusForStackName(stackName, "UPDATE_COMPLETE");

            var deployer = MakeDeployer(null,
                TimeSpan.FromMilliseconds(5),
                TimeSpan.FromMilliseconds(5));

            var sut = new CloudFormationAlarmCreator(deployer, new ConsoleAlarmLogger(false));

            sut.AddAlarms(group, new[] { alarm });

            // act
            await sut.SaveChanges(false);

            // assert
            _cloudFormationMock
               .Verify(x => x.UpdateStackAsync(
                   It.Is<UpdateStackRequest>(s => s.StackName == stackName),
                   It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task SaveChanges_StackDoesNotExist_StackIsCreated()
        {
            // arrange
            var alarm = Alarm();
            var group = Group();
            var stackName = ExpectedStackName(group);

            SetupListStacksToReturnStackNames();
            SetStatusForStackName(stackName, "CREATE_COMPLETE");

            var deployer = MakeDeployer(null,
                TimeSpan.FromMilliseconds(5),
                TimeSpan.FromMilliseconds(5));

            var sut = new CloudFormationAlarmCreator(deployer, new ConsoleAlarmLogger(false));

            sut.AddAlarms(group, new[] { alarm });

            // act
            await sut.SaveChanges(false);

            // assert
            _cloudFormationMock
                .Verify(x => x.CreateStackAsync(
                    It.Is<CreateStackRequest>(s => s.StackName == stackName),
                    It.IsAny<CancellationToken>()));
        }

        [Test]
        public void SaveChanges_CloudformationFails_Throws()
        {
            // arrange
            var alarm = Alarm();

            SetupListStacksToReturnStackNames();
            SetupCreateStackAsyncToFail();

            var deployer = MakeDeployer();

            var sut = new CloudFormationAlarmCreator(deployer, new ConsoleAlarmLogger(false));

            sut.AddAlarms(Group(), new[] { alarm });

            // act
            var ex = Assert.ThrowsAsync<WatchmanException>(() => sut.SaveChanges(false));

            Assert.That(ex.Message, Is.EqualTo("1 stacks failed to deploy"));
        }



        [Test]
        public async Task SaveChanges_NoAlarms_NoStackChangesMade()
        {
            // arrange
            var deployer = MakeDeployer();
            var sut = new CloudFormationAlarmCreator(deployer, new ConsoleAlarmLogger(false));

            // act
            await sut.SaveChanges(false);

            // assert
            _cloudFormationMock
                .Verify(x => x.CreateStackAsync(
                    It.IsAny<CreateStackRequest>(),
                    It.IsAny<CancellationToken>()), Times.Never);

            _cloudFormationMock
                .Verify(x => x.UpdateStackAsync(
                    It.IsAny<UpdateStackRequest>(),
                    It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SaveChanges_DryRun_NoStackChangesMade()
        {
            // arrange
            var alarm = Alarm();

            SetupListStacksToReturnStackNames();

            var deployer = MakeDeployer();
            var sut = new CloudFormationAlarmCreator(deployer, new ConsoleAlarmLogger(false));

            sut.AddAlarms(Group(), new[] { alarm });

            // act
            await sut.SaveChanges(true);

            // assert
            _cloudFormationMock
                .Verify(x => x.CreateStackAsync(
                    It.IsAny<CreateStackRequest>(),
                    It.IsAny<CancellationToken>()), Times.Never());
        }

        [Test]
        public async Task SaveChanges_ChangesToStackSubmitted_WaitsForTargetStatus()
        {
            // arrange
            var alarm = Alarm();
            var group = Group();
            var stackName = ExpectedStackName(group);

            SetupListStacksToReturnStackNames();

            SetupStackStatusSequence(stackName, new List<string> { "CREATE_IN_PROGRESS", "CREATE_IN_PROGRESS", "CREATE_COMPLETE" });

            var statusCheckDelay = TimeSpan.FromMilliseconds(200);

            var deployer = MakeDeployer(null,
                statusCheckDelay, TimeSpan.FromMinutes(5));
            var sut = new CloudFormationAlarmCreator(deployer, new ConsoleAlarmLogger(false));

            sut.AddAlarms(Group(), new[] { alarm });

            var start = DateTime.UtcNow;

            // act
            await sut.SaveChanges(false);

            // assert

            var actualWaitMillis = (DateTime.UtcNow - start).TotalMilliseconds;
            var expectedDelayMillis = statusCheckDelay.TotalMilliseconds * 3;

            Assert.That(actualWaitMillis, Is.GreaterThanOrEqualTo(expectedDelayMillis));

            // this is pretty rough because other parts of the method takes a while, just want to check the time isn't stupidly long
            var maxDelayMillis = expectedDelayMillis * 4;
            Assert.That(actualWaitMillis, Is.LessThanOrEqualTo(maxDelayMillis));

            _cloudFormationMock
                .Verify(x => x.DescribeStacksAsync(
                    It.Is<DescribeStacksRequest>(s => s.StackName == stackName),
                    It.IsAny<CancellationToken>()), Times.Exactly(3));
        }

        [Test]
        public async Task SaveChanges_LargeTemplate_SubmitsViaS3()
        {
            // arrange

            var alarms = Enumerable.Range(0, 120)
                .Select(x => Alarm($"alarm-{x}"))
                .ToList();

            var group = Group();
            var stackName = ExpectedStackName(group);

            SetupListStacksToReturnStackNames();
            SetupStackStatusSequence(stackName, new List<string> { "CREATE_COMPLETE" });

            var s3Location = new S3Location("bucket", "s3/path");

            var deployer = MakeDeployer(s3Location, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            var sut = new CloudFormationAlarmCreator(deployer, new ConsoleAlarmLogger(false));

            // act
            sut.AddAlarms(group, alarms);
            await sut.SaveChanges(false);

            // assert
            var s3Path = $"{s3Location.Path}/{stackName}.json";
            _s3Mock
                .Verify(x => x.PutObjectAsync(
                    It.Is<PutObjectRequest>(r => r.BucketName == s3Location.BucketName && r.Key == s3Path),
                    It.IsAny<CancellationToken>()));

            var expectedStackUrl = $"https://s3.amazonaws.com/{s3Location.BucketName}/{s3Path}";
            _cloudFormationMock
               .Verify(x => x.CreateStackAsync(
                   It.Is<CreateStackRequest>(s => s.StackName == stackName
                        && s.TemplateURL == expectedStackUrl
                        && s.TemplateBody == null),
                   It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Test]
        public async Task SaveChanges_SmallTemplate_SubmitsDirectly()
        {
            // arrange
            var alarm = Alarm();
            var group = Group();
            var stackName = ExpectedStackName(group);

            SetupListStacksToReturnStackNames();
            SetupStackStatusSequence(stackName, new List<string> { "CREATE_COMPLETE" });

            var s3Location = new S3Location("bucket", "s3/path");

            var deployer = MakeDeployer(s3Location, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            var sut = new CloudFormationAlarmCreator(deployer, new ConsoleAlarmLogger(false));

            // act
            sut.AddAlarms(group, new[] { alarm });
            await sut.SaveChanges(false);

            // assert
            _s3Mock
                .Verify(x => x.PutObjectAsync(
                    It.IsAny<PutObjectRequest>(),
                    It.IsAny<CancellationToken>()), Times.Never);

            _cloudFormationMock
               .Verify(x => x.CreateStackAsync(
                   It.Is<CreateStackRequest>(s => s.StackName == stackName
                        && s.TemplateURL == null
                        && !string.IsNullOrWhiteSpace(s.TemplateBody)),
                   It.IsAny<CancellationToken>()), Times.Exactly(1));
        }

        [Test]
        public async Task SaveChanges_ConfigResultsInMultipleStacks_Aborts()
        {
            //arrange
            var alarm1 = Alarm("alarm 1");
            var alarm2 = Alarm("alarm 2");

            // two groups with the same name but different suffix, so that the equality/hash compares will fail
            // but would result in two cloudformation stacks with the same name
            var group1 = Group("name", "suffix1");
            var group2 = Group("name", "suffix2");

            SetupListStacksToReturnStackNames();
            SetupStackStatusSequence(ExpectedStackName(group1), new List<string> { "CREATE_COMPLETE", "CREATE_COMPLETE" });

            var s3Location = new S3Location("bucket", "s3/path");

            var deployer = MakeDeployer(s3Location, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            var sut = new CloudFormationAlarmCreator(deployer, new ConsoleAlarmLogger(false));
            
            //act

            Exception caught = null;
            try
            {
                sut.AddAlarms(group1, new[] { alarm1 });
                sut.AddAlarms(group2, new[] { alarm2 });
                await sut.SaveChanges(false); ;
            }
            catch (Exception ex)
            {
                caught = ex;
            }

            //assert

            Assert.That(caught, Is.Not.Null);
            Assert.That(caught.Message, Contains.Substring("Cannot deploy: multiple stacks would be created with the same name"));
        }

        [Test]
        public async Task SaveChanges_ConfigSetsNumberOfStacksGreaterThan1_DeploysMultipleStacks()
        {
            // arrange
            var alarms = new List<Alarm>();
            for (int i = 0; i < 20; i++)
            {
                alarms.Add(Alarm($"Test alarm {i}"));
            }
           
            var group = Group(numberOfStacks: 2);
            
            var stackName1 = $"Watchman-{group.Name.ToLowerInvariant()}";
            var stackName2 = $"Watchman-{group.Name.ToLowerInvariant()}-1";

            SetupListStacksToReturnStackNames();

            _cloudFormationMock.Setup(x => x.DescribeStacksAsync(
                           It.Is<DescribeStacksRequest>(s => s.StackName == stackName1),
                           It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeStacksResponse
                          {
                              Stacks = new List<Stack>
                                       {
                                           new Stack
                                           {
                                               StackStatus = "CREATE_COMPLETE",
                                               StackName = stackName1
                                           }
                                       }
                          
                         });
            _cloudFormationMock.Setup(x => x.DescribeStacksAsync(
                                          It.Is<DescribeStacksRequest>(s => s.StackName == stackName2),
                                          It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeStacksResponse
                              {
                                  Stacks = new List<Stack>
                                           {
                                               new Stack
                                               {
                                                   StackStatus = "CREATE_COMPLETE",
                                                   StackName = stackName2
                                               }
                                           }

                              });
          

            var deployer = MakeDeployer(null,
                TimeSpan.FromMilliseconds(5),
                TimeSpan.FromMilliseconds(5));

            var sut = new CloudFormationAlarmCreator(deployer, new ConsoleAlarmLogger(false));

            sut.AddAlarms(group,  alarms );

            // act
            await sut.SaveChanges(false);

            // assert
            _cloudFormationMock
                .Verify(x => x.CreateStackAsync(
                            It.Is<CreateStackRequest>(s => s.StackName == stackName1),
                            It.IsAny<CancellationToken>()));

            _cloudFormationMock
                .Verify(x => x.CreateStackAsync(
                            It.Is<CreateStackRequest>(s => s.StackName == stackName2),
                            It.IsAny<CancellationToken>()));
        }

        private CloudFormationStackDeployer MakeDeployer(
            S3Location s3Location, TimeSpan wait, TimeSpan waitTimeout)
        {
            return new CloudFormationStackDeployer(
                new ConsoleAlarmLogger(false),
                _cloudFormationMock.Object,
                _s3Mock.Object,
                s3Location,
                wait, waitTimeout);
        }

        private CloudFormationStackDeployer MakeDeployer()
        {
            return new CloudFormationStackDeployer(
                new ConsoleAlarmLogger(false),
                _cloudFormationMock.Object,
                _s3Mock.Object,
                null);
        }
    }
}
