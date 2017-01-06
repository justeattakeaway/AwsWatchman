using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Engine.Generation.Generic;

namespace Watchman.Engine.Generation.Dynamo
{
    public class OrphanTablesFinder
    {
        private readonly IResourceSource<TableDescription> _tableSource;

        public OrphanTablesFinder(IResourceSource<TableDescription> tableSource)
        {
            _tableSource = tableSource;
        }

        public async Task<OrphansModel> FindOrphanTables(WatchmanConfiguration config)
        {
            var monitoredTables = config.AlertingGroups
                .Where(ag => ! ag.IsCatchAll && ag.DynamoDb?.Tables != null)
                .SelectMany(ag => ag.DynamoDb.Tables)
                .Select(t => t.Name)
                .Distinct();

            var allTables = await _tableSource.GetResourceNamesAsync();

            var unMonitoredTables = allTables
                .Except(monitoredTables)
                .OrderBy(t => t)
                .ToList();

            return new OrphansModel
            {
                Items = unMonitoredTables.ToList(),
                ServiceName = "table"
            };
        }
    }
}
