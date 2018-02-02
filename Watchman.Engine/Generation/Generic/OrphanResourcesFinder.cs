using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Watchman.AwsResources;

namespace Watchman.Engine.Generation.Generic
{
    public class OrphanResourcesFinder<T> where T: class
    {
        private readonly IResourceSource<T> _source;

        public OrphanResourcesFinder(IResourceSource<T> source)
        {
            _source = source;
        }

        public async Task<OrphansModel> FindOrphans(string serviceName,
            IEnumerable<IServiceAlertingGroup> alertingGroups)
        {
            var monitoredResources = alertingGroups
                .Where(ag => !ag.GroupParameters.IsCatchAll)
                .SelectMany(ag => ag.Service.Resources)
                .Select(t => t.Name)
                .Distinct();

            var allResources = await _source.GetResourceNamesAsync();

            var unMonitored = allResources
                .Except(monitoredResources)
                .OrderBy(t => t);

            return new OrphansModel
            {
                Items = unMonitored.ToList(),
                ServiceName = serviceName
            };
        }
    }
}
