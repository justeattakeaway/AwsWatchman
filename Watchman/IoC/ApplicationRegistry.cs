using System.Text.RegularExpressions;
using Amazon.CloudFormation;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using StructureMap;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.DynamoDb;
using Watchman.AwsResources.Services.Sqs;
using Watchman.Configuration.Load;
using Watchman.Engine.Alarms;
using Watchman.Engine.Generation.Dynamo;
using Watchman.Engine.Generation.Dynamo.Alarms;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Generation.Sqs;
using Watchman.Engine.LegacyTracking;
using Watchman.Engine.Logging;
using Watchman.Engine.Sns;

namespace Watchman.IoC
{
    public class ApplicationRegistry : Registry
    {
        public ApplicationRegistry(StartupParameters parameters)
        {
            ConfigureResourceSources();
            ConfigureInternalDependencies(parameters);
        }

        private void ConfigureResourceSources()
        {
            // These cache table names etc
            For<IResourceSource<TableDescription>>()
                .Use<TableDescriptionSource>()
                .Singleton();

            For<IResourceSource<QueueData>>()
                .Use<QueueSource>()
                .Singleton();
        }

        private void ConfigureInternalDependencies(StartupParameters parameters)
        {
            For<ISnsTopicCreator>().Use<SnsTopicCreator>();
            For<ISnsSubscriptionCreator>().Use<SnsSubscriptionCreator>();

            For<ITableAlarmCreator>().Use<TableAlarmCreator>();
            For<IIndexAlarmCreator>().Use<IndexAlarmCreator>();
            For<IAlarmFinder>().Use<AlarmFinder>().Singleton();

            For<IQueueAlarmCreator>().Use<QueueAlarmCreator>();

            For<IDynamoAlarmGenerator>().Use<DynamoAlarmGenerator>();
            For<IOrphanTablesReporter>().Use<OrphanTablesReporter>();
            For<IOrphanQueuesReporter>().Use<OrphanQueuesReporter>();
            For<ISqsAlarmGenerator>().Use<SqsAlarmGenerator>();

            if (!string.IsNullOrWhiteSpace(parameters.WriteCloudFormationTemplatesToDirectory))
            {
                For<ICloudformationStackDeployer>()
                    .Use(
                        ctx => new DummyCloudFormationStackDeployer(
                            parameters.WriteCloudFormationTemplatesToDirectory,
                            ctx.GetInstance<IAlarmLogger>()));
            }
            else
            {
                var s3Location = GetS3Location(parameters);

                For<ICloudformationStackDeployer>()
                    .Use(ctx => new CloudFormationStackDeployer(
                        ctx.GetInstance<IAlarmLogger>(),
                        ctx.GetInstance<IAmazonCloudFormation>(),
                        ctx.GetInstance<IAmazonS3>(),
                        s3Location
                    ));
            }

            For<IAlarmCreator>().Use<CloudFormationAlarmCreator>();

            var fileSettings = new FileSettings(parameters.ConfigFolderLocation);

            For<FileSettings>().Use(fileSettings);

            For<ILegacyAlarmTracker>().Use<LegacyAlarmTracker>().Singleton();
            For<IOrphanedAlarmReporter>().Use<OrphanedAlarmReporter>();
        }

        private static S3Location GetS3Location(StartupParameters parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters.TemplateS3Path))
            {
                return null;
            }

            var regex = new Regex("^s3://([^/]+)/(.*)", RegexOptions.IgnoreCase);
            var match = regex.Match(parameters.TemplateS3Path);
            if (!match.Success)
            {
                throw new Exception("Parameter TemplateS3Path does not match format s3://bucket/path");
            }

            return new S3Location(match.Groups[1].Value, match.Groups[2].Value);
        }
    }
}
