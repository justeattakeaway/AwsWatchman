using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Watchman.AwsResources.Services.DynamoDb
{
    public class TableDescriptionSource : IResourceSource<TableDescription>
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private IList<string> _tableNames;
        private readonly Dictionary<string, TableDescription> _cachedTableDescriptions
            = new Dictionary<string, TableDescription>();

        public TableDescriptionSource(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
        }

        public async Task<IList<AwsResource<TableDescription>>> GetResourcesAsync()
        {
            await CheckTableNamesLoaded();

            return _tableNames
                .Select(t => new AwsResource<TableDescription>(t,
                    item => GetResourceAsync(item.Name))
                )
                .ToList();
        }

        public async Task<IList<string>> GetResourceNamesAsync()
        {
            return (await GetResourcesAsync())
                .Select(r => r.Name)
                .ToArray();
        }

        public async Task<TableDescription> GetResourceAsync(string name)
        {
            await CheckTableNamesLoaded();

            if (!_tableNames.Contains(name))
            {
                return null;
            }

            if (_cachedTableDescriptions.ContainsKey(name))
            {
                return _cachedTableDescriptions[name];
            }

            DescribeTableResponse tableResponse;

            try
            {
                tableResponse = await _dynamoDb.DescribeTableAsync(name);
            }
            catch (ResourceNotFoundException)
            {
                return null;
            }

            _cachedTableDescriptions.Add(tableResponse.Table.TableName, tableResponse.Table);

            return tableResponse.Table;
        }

        private async Task CheckTableNamesLoaded()
        {
            if (_tableNames == null)
            {
                _tableNames = await ReadTableNames();
            }
        }

        private async Task<IList<string>> ReadTableNames()
        {
            var tableNames = new List<string>();
            string lastTableName = null;
            do
            {
                var tableResponse = await _dynamoDb.ListTablesAsync(lastTableName);
                if (tableResponse != null)
                {
                    tableNames.AddRange(tableResponse.TableNames);
                    lastTableName = tableResponse.LastEvaluatedTableName;
                }
                else
                {
                    lastTableName = null;
                }
            } while (lastTableName != null);

            return tableNames;
        }
    }
}
