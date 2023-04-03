using Watchman.Configuration;
using Watchman.Engine.Generation.Generic;

namespace Watchman.Engine.Generation.Sqs
{
    public class OrphanQueuesReporter : IOrphanQueuesReporter
    {
        private readonly OrphanQueuesFinder _orphanQueuesFinder;
        private readonly OrphansLogger _logger;

        public OrphanQueuesReporter(OrphanQueuesFinder orphanQueuesFinder, OrphansLogger logger)
        {
            _orphanQueuesFinder = orphanQueuesFinder;
            _logger = logger;
        }

        public async Task FindAndReport(WatchmanConfiguration config)
        {
            var orphans = await _orphanQueuesFinder.FindOrphans(config);
            _logger.Log(orphans);
        }
    }
}
