using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.S3;
using Amazon.S3.Model;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Logging;
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

        private IAmazonCloudFormation _cloudFormationMock;
        private IAmazonS3 _s3Mock;

        private void SetupListStacksToReturnStackNames(params string[] stackNames)
        {
            _cloudFormationMock
                .ListStacksAsync(Arg.Any<ListStacksRequest>(), Arg.Any<CancellationToken>())
                .Returns(new ListStacksResponse
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
                .CreateStackAsync(Arg.Any<CreateStackRequest>(), Arg.Any<CancellationToken>())
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
                .DescribeStacksAsync(
                    Arg.Is<DescribeStacksRequest>(s => s.StackName == stackName),
                    Arg.Any<CancellationToken>())
                .Returns(_ =>
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
                ResourceIdentifier = $"resource-for-{name}",
                Dimensions = new List<Dimension>()
            };
        }

        [SetUp]
        public void SetUp()
        {
            _cloudFormationMock = Substitute.For<IAmazonCloudFormation>();
            _s3Mock = Substitute.For<IAmazonS3>();
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
            await _cloudFormationMock
                .Received()
                .UpdateStackAsync(
                    Arg.Is<UpdateStackRequest>(s => s.StackName == stackName),
                    Arg.Any<CancellationToken>());
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
            await _cloudFormationMock
                .Received()
                .CreateStackAsync(
                    Arg.Is<CreateStackRequest>(s => s.StackName == stackName),
                    Arg.Any<CancellationToken>());
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
            await _cloudFormationMock
                .DidNotReceive()
                .CreateStackAsync(
                    Arg.Any<CreateStackRequest>(),
                    Arg.Any<CancellationToken>());

            await _cloudFormationMock
                .DidNotReceive()
                .UpdateStackAsync(
                    Arg.Any<UpdateStackRequest>(),
                    Arg.Any<CancellationToken>());
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
            await _cloudFormationMock
                .DidNotReceive()
                .CreateStackAsync(
                    Arg.Any<CreateStackRequest>(),
                    Arg.Any<CancellationToken>());
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

            await _cloudFormationMock
                .Received(3)
                .DescribeStacksAsync(
                    Arg.Is<DescribeStacksRequest>(s => s.StackName == stackName),
                    Arg.Any<CancellationToken>());
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
            await _s3Mock
                .PutObjectAsync(
                    Arg.Is<PutObjectRequest>(r => r.BucketName == s3Location.BucketName && r.Key == s3Path),
                    Arg.Any<CancellationToken>());

            var expectedStackUrl = $"https://s3.amazonaws.com/{s3Location.BucketName}/{s3Path}";
            await _cloudFormationMock
                .Received(1)
                .CreateStackAsync(
                    Arg.Is<CreateStackRequest>(s => s.StackName == stackName
                                                    && s.TemplateURL == expectedStackUrl
                                                    && s.TemplateBody == null),
                    Arg.Any<CancellationToken>());
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
            await _s3Mock
                .DidNotReceive()
                .PutObjectAsync(
                    Arg.Any<PutObjectRequest>(),
                    Arg.Any<CancellationToken>());

            await _cloudFormationMock
                .Received(1)
                .CreateStackAsync(
                    Arg.Is<CreateStackRequest>(s => s.StackName == stackName
                                                    && s.TemplateURL == null
                                                    && !string.IsNullOrWhiteSpace(s.TemplateBody)),
                    Arg.Any<CancellationToken>());
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

            _cloudFormationMock.DescribeStacksAsync(
                           Arg.Is<DescribeStacksRequest>(s => s.StackName == stackName1),
                           Arg.Any<CancellationToken>())
                .Returns(new DescribeStacksResponse
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
            _cloudFormationMock.DescribeStacksAsync(
                                          Arg.Is<DescribeStacksRequest>(s => s.StackName == stackName2),
                                          Arg.Any<CancellationToken>())
                .Returns(new DescribeStacksResponse
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
            await _cloudFormationMock
                .Received()
                .CreateStackAsync(
                            Arg.Is<CreateStackRequest>(s => s.StackName == stackName1),
                            Arg.Any<CancellationToken>());

            await _cloudFormationMock
                .Received()
                .CreateStackAsync(
                            Arg.Is<CreateStackRequest>(s => s.StackName == stackName2),
                            Arg.Any<CancellationToken>());
        }

        private CloudFormationStackDeployer MakeDeployer(
            S3Location s3Location, TimeSpan wait, TimeSpan waitTimeout)
        {
            return new CloudFormationStackDeployer(
                new ConsoleAlarmLogger(false),
                _cloudFormationMock,
                _s3Mock,
                s3Location,
                wait, waitTimeout);
        }

        private CloudFormationStackDeployer MakeDeployer()
        {
            return new CloudFormationStackDeployer(
                new ConsoleAlarmLogger(false),
                _cloudFormationMock,
                _s3Mock,
                null);
        }
    }
}
