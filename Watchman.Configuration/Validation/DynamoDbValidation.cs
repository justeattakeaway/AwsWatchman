namespace Watchman.Configuration.Validation
{
    static class DynamoDbValidation
    {
        public static void Validate(string alertingGroupName, DynamoDb dynamoDb)
        {
            if (dynamoDb.Threshold.HasValue)
            {
                ValidTableThreshold(dynamoDb.Threshold.Value);
            }

            if (dynamoDb.ThrottlingThreshold <= 0)
            {
                throw new ConfigException($"AlertingGroup '{alertingGroupName}' has DynamoDb with an invalid throttling threshold of {dynamoDb.ThrottlingThreshold}");
            }

            foreach (var table in dynamoDb.Tables)
            {
                ValidateTable(alertingGroupName, table);
            }
        }

        private static void ValidateTable(string alertingGroupName, Table table)
        {
            if (table == null)
            {
                throw new ConfigException($"AlertingGroup '{alertingGroupName}' has a null table");
            }

            if (string.IsNullOrWhiteSpace(table.Name) && string.IsNullOrWhiteSpace(table.Pattern))
            {
                throw new ConfigException($"AlertingGroup '{alertingGroupName}' has a table with no name or pattern");
            }

            if (!string.IsNullOrWhiteSpace(table.Name) && !string.IsNullOrWhiteSpace(table.Pattern))
            {
                throw new ConfigException($"AlertingGroup '{alertingGroupName}' has a table '{table.Name}' with a name and a pattern");
            }

            if (table.Threshold.HasValue)
            {
                ValidTableThreshold(table.Threshold.Value);
            }

            if (table.ThrottlingThreshold.HasValue && table.ThrottlingThreshold <= 0)
            {
                throw new ConfigException($"Throttling threshold of '{table.ThrottlingThreshold}' must be greater than zero");
            }

        }

        private static void ValidTableThreshold(double threshold)
        {
            if (threshold <= 0.0)
            {
                throw new ConfigException($"Threshold of '{threshold}' must be greater than zero");
            }

            if (threshold > 1.0)
            {
                throw new ConfigException($"Threshold of '{threshold}' must be less than or equal to one");
            }
        }
    }
}
