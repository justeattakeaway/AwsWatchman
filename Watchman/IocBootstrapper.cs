using System;
using System.Text.RegularExpressions;
using Amazon.DynamoDBv2.Model;
using StructureMap;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.DynamoDb;
using Watchman.AwsResources.Services.Sqs;
using Watchman.Configuration;
using Watchman.Configuration.Load;
using Watchman.Engine.Alarms;
using Watchman.Engine.Generation.Dynamo.Alarms;
using Watchman.Engine.Generation.Sqs;
using Watchman.Engine.Logging;
using Watchman.Engine.Sns;
using Amazon.CloudFormation;
using Amazon.S3;
using Watchman.Engine.Generation.Dynamo;
using Watchman.Engine.Generation.Generic;

namespace Watchman
{
    public class IocBootstrapper
    {
        public IContainer ConfigureContainer(StartupParameters parameters)
        {
            var container = new Container();
            container.Configure(ctx => AwsBootstrapper.Configure(ctx, parameters));
            container.Configure(AwsServiceBootstrapper.Configure);
            container.Configure(ctx => ConfigureLoggers(ctx, parameters));
            container.Configure(ConfigureResourceSources);
            container.Configure(ctx => ConfigureInternalDependencies(ctx, parameters));
            return container;
        }

        private static void ConfigureLoggers(IProfileRegistry registry, StartupParameters parameters)
        {
            var fileSettings = new FileSettings(parameters.ConfigFolderLocation);
            var alarmLogger = new ConsoleAlarmLogger(parameters.Verbose);
            var loadLogger = new ConsoleConfigLoadLogger(parameters.Verbose);

            registry.For<FileSettings>().Use(fileSettings);
            registry.For<IAlarmLogger>().Use(alarmLogger);
            registry.For<IConfigLoadLogger>().Use(loadLogger);
        }

        private static void ConfigureResourceSources(IProfileRegistry registry)
        {
            registry.For<IResourceSource<TableDescription>>().Use<TableDescriptionSource>();
            registry.For<IResourceSource<QueueData>>().Use<QueueSource>();
        }

        private static void ConfigureInternalDependencies(IProfileRegistry registry, StartupParameters parameters)
        {
            registry.For<ISnsTopicCreator>().Use<SnsTopicCreator>();
            registry.For<ISnsSubscriptionCreator>().Use<SnsSubscriptionCreator>();

            registry.For<ITableAlarmCreator>().Use<TableAlarmCreator>();
            registry.For<IIndexAlarmCreator>().Use<IndexAlarmCreator>();
            registry.For<IAlarmFinder>().Use<AlarmFinder>();

            registry.For<IConfigLoader>().Use<ConfigLoader>();

            registry.For<IQueueAlarmCreator>().Use<QueueAlarmCreator>();

            registry.For<IDynamoAlarmGenerator>().Use<DynamoAlarmGenerator>();
            registry.For<IOrphanTablesReporter>().Use<OrphanTablesReporter>();
            registry.For<IOrphanQueuesReporter>().Use<OrphanQueuesReporter>();
            registry.For<ISqsAlarmGenerator>().Use<SqsAlarmGenerator>();

            if (!string.IsNullOrWhiteSpace(parameters.WriteCloudFormationTemplatesToDirectory))
            {
                registry
                    .For<ICloudformationStackDeployer>()
                    .Use(
                        ctx => new DummyCloudFormationStackDeployer(
                            parameters.WriteCloudFormationTemplatesToDirectory,
                            ctx.GetInstance<IAlarmLogger>()));
            }
            else
            {
                var s3Location = GetS3Location(parameters);

                registry
                    .For<ICloudformationStackDeployer>()
                    .Use(ctx => new CloudformationStackDeployer(
                        ctx.GetInstance<IAlarmLogger>(), 
                        ctx.GetInstance<IAmazonCloudFormation>(),
                        ctx.GetInstance<IAmazonS3>(), 
                        s3Location
                    ));
            }

            registry.For<IAlarmCreator>().Use<CloudFormationAlarmCreator>();
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
