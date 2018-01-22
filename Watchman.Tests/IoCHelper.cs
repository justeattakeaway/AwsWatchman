using System;
using System.Collections.Generic;
using Moq;
using StructureMap.Diagnostics;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Load;
using Watchman.Engine;
using Watchman.Engine.Generation;
using Watchman.Engine.Generation.Dynamo;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Generation.Sqs;
using Watchman.Engine.Logging;

namespace Watchman.Tests
{
    class IoCHelper
    {
        public static AlarmLoaderAndGenerator CreateSystemUnderTest<T>(
            IResourceSource<T> source,
            IAlarmDimensionProvider<T> dimensionProvider, 
            IResourceAttributesProvider<T> attributeProvider,
            Func<WatchmanConfiguration, WatchmanServiceConfiguration> mapper,
            IAlarmCreator creator,
            IConfigLoader loader
        ) where T: class
        {
            var builder = new Builder(loader, creator);
            builder.AddService(source, dimensionProvider, attributeProvider, mapper);
            return builder.Build();
        }
    }

    class Builder
    {
        private readonly IConfigLoader _loader;
        private readonly IAlarmCreator _creator;

        private readonly IAlarmLogger _logger = new Mock<IAlarmLogger>().Object;

        private readonly List<IServiceAlarmTasks> _serviceAlarmTasks = new List<IServiceAlarmTasks>();

        public Builder(IConfigLoader loader,
            IAlarmCreator creator)
        {
            _loader = loader;
            _creator = creator;
        }

        public AlarmLoaderAndGenerator Build()
        {

            return new AlarmLoaderAndGenerator(
                _logger,
                _loader,
                new Mock<IDynamoAlarmGenerator>().Object,
                new Mock<IOrphanTablesReporter>().Object,
                new Mock<ISqsAlarmGenerator>().Object,
                new Mock<IOrphanQueuesReporter>().Object,
                _creator,
                _serviceAlarmTasks
            );
        }

        public void AddService<T>(IResourceSource<T> source,
            IAlarmDimensionProvider<T> dimensionProvider,
            IResourceAttributesProvider<T> attributeProvider,
            Func<WatchmanConfiguration, WatchmanServiceConfiguration> mapper) where T : class
        {
            var task = new ServiceAlarmTasks<T>(
                _logger,
                new ResourceNamePopulator<T>(_logger, source),
                new ServiceAlarmGenerator<T>(
                    _creator,
                    new ServiceAlarmBuilder<T>(source, dimensionProvider, attributeProvider)),
                new OrphanResourcesReporter<T>(
                    new OrphanResourcesFinder<T>(source),
                    new OrphansLogger(_logger)),
                mapper
            );

            _serviceAlarmTasks.Add(task);
        }
    }
}
