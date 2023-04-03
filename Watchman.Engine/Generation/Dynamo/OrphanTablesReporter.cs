using Watchman.Configuration;
using Watchman.Engine.Generation.Generic;

namespace Watchman.Engine.Generation.Dynamo
{
    public class OrphanTablesReporter : IOrphanTablesReporter
    {
        private readonly OrphanTablesFinder _orphanTablesFinder;
        private readonly OrphansLogger _logger;

        public OrphanTablesReporter(OrphanTablesFinder orphanTablesFinder, OrphansLogger logger)
        {
            _orphanTablesFinder = orphanTablesFinder;
            _logger = logger;
        }

        public async Task FindAndReport(WatchmanConfiguration config)
        {
            var orphans = await _orphanTablesFinder.FindOrphanTables(config);
            _logger.Log(orphans);
        }
    }
}
