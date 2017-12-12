using System;
using Moq;
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
        ) where T:class
        {
            var fakeLogger = new Mock<IAlarmLogger>();

            var task = new ServiceAlarmTasks<T>(
                fakeLogger.Object,
                new ResourceNamePopulator<T>(fakeLogger.Object, source),
                new ServiceAlarmGenerator<T>(
                    creator, 
                    new ServiceAlarmBuilder<T>(source, dimensionProvider, attributeProvider)),
                new OrphanResourcesReporter<T>(
                    new OrphanResourcesFinder<T>(source),
                    new OrphansLogger(fakeLogger.Object)),
                mapper
            );

            return new AlarmLoaderAndGenerator(
                fakeLogger.Object,
                loader,
                new Mock<IDynamoAlarmGenerator>().Object,
                new Mock<IOrphanTablesReporter>().Object,
                new Mock<ISqsAlarmGenerator>().Object,
                new Mock<IOrphanQueuesReporter>().Object,
                creator,
                new[] {task}
            );
        }
    }
}