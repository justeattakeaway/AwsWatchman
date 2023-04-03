using Amazon.DynamoDBv2.Model;
using StructureMap;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.DynamoDb;
using Watchman.Configuration;
using Watchman.Configuration.Load;
using Watchman.Engine.Logging;

namespace Quartermaster
{
    public class IocBootstrapper
    {
        public IContainer ConfigureContainer(StartupParameters parameters)
        {
            var container = new Container();
            container.Configure(ctx => AwsBootstrapper.Configure(ctx, parameters));
            container.Configure(ctx => ConfigureLoggers(ctx, parameters));
            container.Configure(ConfigureInternalDependencies);

            return container;
       }

        private void ConfigureLoggers(IProfileRegistry registry, StartupParameters parameters)
        {
            var fileSettings = new FileSettings(parameters.ConfigFolderLocation);
            var loadLogger = new ConsoleConfigLoadLogger(parameters.Verbose);
            var alarmLogger = new ConsoleAlarmLogger(parameters.Verbose);

            registry.For<FileSettings>().Use(fileSettings);
            registry.For<IConfigLoadLogger>().Use(loadLogger);
            registry.For<IAlarmLogger>().Use(alarmLogger);
        }

        public static void ConfigureInternalDependencies(IProfileRegistry registry)
        {
            registry.For<IResourceSource<TableDescription>>().Use<TableDescriptionSource>();
            registry.For<IConfigLoader>().Use<ConfigLoader>();
        }
    }
}
