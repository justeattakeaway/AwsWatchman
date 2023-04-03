﻿using Amazon.AutoScaling;
using Amazon.CloudFormation;
using Amazon.CloudFront;
using Amazon.CloudWatch;
using Amazon.DAX;
using Amazon.DynamoDBv2;
using Amazon.EC2;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancingV2;
using Amazon.Lambda;
using Amazon.RDS;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.StepFunctions;
using Moq;
using StructureMap;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Load;
using Watchman.Engine.Logging;

namespace Watchman.Tests.IoC
{
    class FakeBoundaryRegistry : Registry
    {
        private Mock<T> SetupFake<T>() where T : class
        {
            var fake = new Mock<T>();
            For<T>()
                .Use(fake.Object)
                .Singleton();
            return fake;
        }

        public FakeBoundaryRegistry()
        {
            SetupLocalDependencies();
            SetupAwsDependencies();
        }

        private void SetupAwsDependencies()
        {
            SetupFake<IAmazonDynamoDB>();
            SetupFake<IAmazonCloudWatch>();

            SetupFake<IAmazonSimpleNotificationService>()
                // basic setup to stop other tests blowing up
                .Setup(x => x.CreateTopicAsync(It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateTopicResponse()
                {
                    TopicArn = "test-arn"
                });

            SetupFake<IAmazonRDS>();
            SetupFake<IAmazonAutoScaling>();
            SetupFake<IAmazonCloudFormation>();
            SetupFake<IAmazonLambda>();
            SetupFake<IAmazonEC2>();
            SetupFake<IAmazonElasticLoadBalancing>();
            SetupFake<IAmazonElasticLoadBalancingV2>();
            SetupFake<IAmazonS3>();
            SetupFake<IAmazonStepFunctions>();
            SetupFake<IAmazonDAX>();
            SetupFake<IAmazonCloudWatch>();
            SetupFake<IAmazonCloudFront>();
        }

        private void SetupLocalDependencies()
        {
            SetupFake<IAlarmLogger>();
            SetupFake<IConfigLoadLogger>();
            SetupFake<IConfigLoader>();
            SetupFake<ICurrentTimeProvider>();
        }
    }
}
