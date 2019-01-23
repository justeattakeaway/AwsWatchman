using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Watchman.AwsResources.Services.DynamoDb
{
    public class TableDescriptionSource : IResourceSource<TableDescription>
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private IList<string> _tableNames;
        private readonly Dictionary<string, AwsResource<TableDescription>> _cachedTableDescriptions = new Dictionary<string, AwsResource<TableDescription>>();

        public TableDescriptionSource(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
        }

        public async Task<IList<string>> GetResourceNamesAsync()
        {
            if (_tableNames == null)
            {
                _tableNames = await ReadTableNames();
            }

            return _tableNames;
        }

        public async Task<AwsResource<TableDescription>> GetResourceAsync(string name)
        {
            if (_tableNames == null)
            {
                await GetResourceNamesAsync();
            }

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

            var dataItem = new AwsResource<TableDescription>(tableResponse.Table.TableName, tableResponse.Table);
            _cachedTableDescriptions.Add(tableResponse.Table.TableName, dataItem);

            return dataItem;
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
