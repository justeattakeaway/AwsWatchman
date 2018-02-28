using Amazon.AutoScaling;
using Amazon.CloudFormation;
using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.EC2;
using Amazon.ElasticLoadBalancing;
using Amazon.Lambda;
using Amazon.RDS;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.StepFunctions;
using Moq;
using StructureMap;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Load;
using Watchman.Engine.Logging;

namespace Watchman.Tests.IoC
{class FakeBoundaryRegistry : Registry
    {
        private void SetupFake<T>() where T:class
        {
            For<T>()
                .Use(Mock.Of<T>())
                .Singleton();
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
            SetupFake<IAmazonSimpleNotificationService>();
            SetupFake<IAmazonRDS>();
            SetupFake<IAmazonAutoScaling>();
            SetupFake<IAmazonCloudFormation>();
            SetupFake<IAmazonLambda>();
            SetupFake<IAmazonEC2>();
            SetupFake<IAmazonElasticLoadBalancing>();
            SetupFake<IAmazonS3>();
            SetupFake<IAmazonStepFunctions>();
            SetupFake<IAmazonCloudWatch>();
        }

        private void SetupLocalDependencies()
        {
            SetupFake<IConfigLoader>();
            SetupFake<IAlarmLogger>();
            SetupFake<IConfigLoadLogger>();
            SetupFake<ICurrentTimeProvider>();
        }
    }
}
