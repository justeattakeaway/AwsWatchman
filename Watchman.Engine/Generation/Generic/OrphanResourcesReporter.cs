using System.Collections.Generic;
using System.Threading.Tasks;

namespace Watchman.Engine.Generation.Generic
{
    public class OrphanResourcesReporter<T> where T : class
    {
        private readonly OrphanResourcesFinder<T> _orphansFinder;
        private readonly OrphansLogger _logger;

        public OrphanResourcesReporter(
            OrphanResourcesFinder<T> orphansFinder,
            OrphansLogger logger)
        {
            _orphansFinder = orphansFinder;
            _logger = logger;
        }

        public async Task FindAndReport(string serviceName, IEnumerable<ServiceAlertingGroup> alertingGroups)
        {
            var orphans = await _orphansFinder.FindOrphans(serviceName, alertingGroups);
            _logger.Log(orphans);
        }
    }
}
