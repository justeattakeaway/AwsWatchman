using System.Collections.Generic;
using System.Threading.Tasks;

namespace Watchman.Engine.Generation.Generic
{
    public class OrphanResourcesReporter<T, TConfig> where T : class
    where TConfig : class
    {
        private readonly OrphanResourcesFinder<T, TConfig> _orphansFinder;
        private readonly OrphansLogger _logger;

        public OrphanResourcesReporter(
            OrphanResourcesFinder<T, TConfig> orphansFinder,
            OrphansLogger logger)
        {
            _orphansFinder = orphansFinder;
            _logger = logger;
        }

        public async Task FindAndReport(string serviceName, IEnumerable<PopulatedServiceAlertingGroup<TConfig,T>> alertingGroups)
        {
            var orphans = await _orphansFinder.FindOrphans(serviceName, alertingGroups);
            _logger.Log(orphans);
        }
    }
}
