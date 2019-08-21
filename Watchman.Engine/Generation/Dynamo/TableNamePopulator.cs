using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.DynamoDBv2.Model;
using Watchman.AwsResources;
using Watchman.AwsResources.Services.DynamoDb;
using Watchman.Configuration;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation.Dynamo
{
    public class TableNamePopulator
    {
        private readonly IAlarmLogger _logger;
        private readonly IResourceSource<TableDescription> _tableSource;

        public TableNamePopulator(IAlarmLogger logger,
            IResourceSource<TableDescription> tableSource)
        {
            _logger = logger;
            _tableSource = tableSource;
        }

        public async Task PopulateDynamoTableNames(AlertingGroup alertingGroup)
        {
            alertingGroup.DynamoDb.Tables = await ExpandTablePatterns(alertingGroup.DynamoDb, alertingGroup.Name);
        }

        private async Task<List<Table>> ExpandTablePatterns(DynamoDb dynamoDb, string alertingGroupName)
        {
            var tablesWithoutPatterns = dynamoDb.Tables
                .Where(t => string.IsNullOrWhiteSpace(t.Pattern))
                .ToList();

            var patterns = dynamoDb.Tables
                .Where(t => !string.IsNullOrWhiteSpace(t.Pattern))
                .ToList();

            var tablesFromPatterns = new List<Table>();

            foreach (var tablePattern in patterns)
            {
                var matches = await GetPatternMatches(tablePattern, alertingGroupName);

                // filter out duplicates
                matches = matches
                    .Where(match => tablesFromPatterns.All(t => t.Name != match.Name))
                    .ToList();

                matches = matches
                    .Where(match => tablesWithoutPatterns.All(t => t.Name != match.Name))
                    .ToList();

                tablesFromPatterns.AddRange(matches);
            }

            return tablesWithoutPatterns
                .Union(tablesFromPatterns)
                .ToList();
        }
        private async Task<IList<Table>> GetPatternMatches(Table tablePattern, string alertingGroupName)
        {
            var tableNames = await _tableSource.GetResourceNamesAsync();

            var matches = tableNames
                .WhereRegexIsMatch(tablePattern.Pattern)
                .Select(tn => PatternToTable(tablePattern, tn))
                .ToList();

            if (matches.Count == 0)
            {
                _logger.Info($"{alertingGroupName} pattern '{tablePattern.Pattern}' matched no table names");
            }
            else if (matches.Count == tableNames.Count)
            {
                _logger.Info($"{alertingGroupName} pattern '{tablePattern.Pattern}' matched all table names");
            }
            else
            {
                _logger.Detail($"{alertingGroupName}  pattern '{tablePattern.Pattern}' matched {matches.Count} of {tableNames.Count} table names");
            }

            return matches;
        }

        private static Table PatternToTable(Table pattern, string name)
        {
            return new Table
            {
                Name = name,
                Pattern = null,
                Threshold = pattern.Threshold,
                MonitorWrites = pattern.MonitorWrites,
                MonitorThrottling = pattern.MonitorThrottling,
                ThrottlingThreshold = pattern.ThrottlingThreshold
            };
        }
    }
}
