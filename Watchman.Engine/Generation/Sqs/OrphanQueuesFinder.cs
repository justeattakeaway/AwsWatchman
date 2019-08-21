using System.Linq;
using System.Threading.Tasks;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.DynamoDb;
using Watchman.AwsResources.Services.Sqs;
using Watchman.Configuration;
using Watchman.Engine.Generation.Generic;

namespace Watchman.Engine.Generation.Sqs
{
    public class OrphanQueuesFinder
    {
        private readonly IResourceSource<QueueData> _queueSource;

        public OrphanQueuesFinder(IResourceSource<QueueData> queueSource)
        {
            _queueSource = queueSource;
        }

        public async Task<OrphansModel> FindOrphans(WatchmanConfiguration config)
        {
            var monitoredQueues = config.AlertingGroups
                .Where(ag => ! ag.IsCatchAll && ag.Sqs?.Queues != null)
                .SelectMany(ag => ag.Sqs.Queues)
                .Select(t => t.Name)
                .Distinct();

            var allQueues = await _queueSource.GetResourceNamesAsync();

            var unMonitoredQueues = allQueues
                .Except(monitoredQueues)
                .OrderBy(t => t)
                .ToList();

            return new OrphansModel
            {
                Items = unMonitoredQueues.ToList(),
                ServiceName = "queue"
            };
        }
    }
}
