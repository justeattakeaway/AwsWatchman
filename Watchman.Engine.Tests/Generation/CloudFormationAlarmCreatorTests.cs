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
        private string ExpectedStackName(ServiceAlertingGroup group)
        {
            return $"Watchman-{group.Name.ToLowerInvariant()}";
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
                AlertingGroup = new ServiceAlertingGroup
                {
                    Name = "group-name",
                    AlarmNameSuffix = "group-suffix"
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
            var stackName = ExpectedStackName(alarm.AlertingGroup);

            SetupListStacksToReturnStackNames(stackName);
            SetStatusForStackName(stackName, "UPDATE_COMPLETE");

            var sut = new CloudFormationAlarmCreator(
                new CloudformationStackDeployer(
                    new ConsoleAlarmLogger(false), 
                    _cloudFormationMock.Object, 
                    _s3Mock.Object, 
                    null,
                    TimeSpan.FromMilliseconds(5), 
                    TimeSpan.FromMilliseconds(5))
                );

            sut.AddAlarm(alarm);

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
            var stackName = ExpectedStackName(alarm.AlertingGroup);

            SetupListStacksToReturnStackNames();
            SetStatusForStackName(stackName, "CREATE_COMPLETE");

            var sut = new CloudFormationAlarmCreator(
                new CloudformationStackDeployer(
                    new ConsoleAlarmLogger(false), 
                    _cloudFormationMock.Object, 
                    _s3Mock.Object, 
                    null,
                    TimeSpan.FromMilliseconds(5), 
                    TimeSpan.FromMilliseconds(5)
                    ));

            sut.AddAlarm(alarm);

            // act
            await sut.SaveChanges(false);

            // assert
            _cloudFormationMock
                .Verify(x => x.CreateStackAsync(
                    It.Is<CreateStackRequest>(s => s.StackName == stackName),
                    It.IsAny<CancellationToken>()));
        }

        [Test]
        public async Task SaveChanges_NoAlarms_NoStackChangesMade()
        {
            // arrange
            var sut = new CloudFormationAlarmCreator(
                new CloudformationStackDeployer(
                    new ConsoleAlarmLogger(false), 
                    _cloudFormationMock.Object,
                    _s3Mock.Object, 
                    null
                    ));

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

            var sut = new CloudFormationAlarmCreator(
                new CloudformationStackDeployer(
                    new ConsoleAlarmLogger(false), 
                    _cloudFormationMock.Object, 
                    _s3Mock.Object, 
                    null));

            sut.AddAlarm(alarm);

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
            var stackName = ExpectedStackName(alarm.AlertingGroup);

            SetupListStacksToReturnStackNames();

            SetupStackStatusSequence(stackName, new List<string> { "CREATE_IN_PROGRESS", "CREATE_IN_PROGRESS", "CREATE_COMPLETE" });

            var statusCheckDelay = TimeSpan.FromMilliseconds(200);
            var sut = new CloudFormationAlarmCreator(
                new CloudformationStackDeployer(
                    new ConsoleAlarmLogger(false), 
                    _cloudFormationMock.Object, 
                    _s3Mock.Object, 
                    null,
                    statusCheckDelay, 
                    TimeSpan.FromMinutes(5)));
            sut.AddAlarm(alarm);

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

            var stackName = ExpectedStackName(alarms.First().AlertingGroup);

            SetupListStacksToReturnStackNames();
            SetupStackStatusSequence(stackName, new List<string> { "CREATE_COMPLETE" });

            var s3Location = new S3Location("bucket", "s3/path");

            var sut = new CloudFormationAlarmCreator(
                new CloudformationStackDeployer(
                    new ConsoleAlarmLogger(false), 
                    _cloudFormationMock.Object, 
                    _s3Mock.Object, 
                    s3Location,
                    TimeSpan.Zero,
                    TimeSpan.FromMinutes(1)
                    ));

            foreach (var alarm in alarms)
            {
                sut.AddAlarm(alarm);
            }

            // act
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
            var stackName = ExpectedStackName(alarm.AlertingGroup);

            SetupListStacksToReturnStackNames();
            SetupStackStatusSequence(stackName, new List<string> { "CREATE_COMPLETE" });

            var s3Location = new S3Location("bucket", "s3/path");

            var sut = new CloudFormationAlarmCreator(
                new CloudformationStackDeployer(
                    new ConsoleAlarmLogger(false), 
                    _cloudFormationMock.Object, 
                    _s3Mock.Object, 
                    s3Location,
                    TimeSpan.Zero,
                    TimeSpan.FromMinutes(1)));

            sut.AddAlarm(alarm);

            // act
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
    }
}
