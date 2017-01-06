using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.DynamoDBv2.Model;
using Watchman.Configuration;
using Watchman.Configuration.Load;
using QuarterMaster.Models;
using Watchman.AwsResources;
using Watchman.Configuration.Validation;
using Watchman.Engine;
using Watchman.Engine.Generation.Dynamo;

namespace QuarterMaster
{
    public class ReportGenerator
    {
        private const int ReportDuration = 6;
        private readonly IConfigLoader _configLoader;
        private readonly TableNamePopulator _tableNamePopulator;
        private readonly IResourceSource<TableDescription> _tableSource;
        private readonly IAmazonCloudWatch _cloudwatch;

        public ReportGenerator(
            IConfigLoader configLoader,
            TableNamePopulator tableNamePopulator,
            IResourceSource<TableDescription> tableSource,
            IAmazonCloudWatch cloudwatch)
        {
            _configLoader = configLoader;
            _tableNamePopulator = tableNamePopulator;
            _tableSource = tableSource;
            _cloudwatch = cloudwatch;
        }

        public async Task<IList<ProvisioningReport>> GetReports()
        {
            try
            {
                Console.WriteLine("Starting");

                var config = _configLoader.LoadConfig();
                ConfigValidator.Validate(config);
                foreach (var alertingGroup in config.AlertingGroups)
                {
                    await _tableNamePopulator.PopulateDynamoTableNames(alertingGroup);
                }

                var groupsRequiringReports = config.AlertingGroups
                    .Where(x => x.ReportTargets.Any());

                var provisioningReports = new List<ProvisioningReport>();
                foreach (var alertingGroup in groupsRequiringReports)
                {
                    var report = await GenerateAlertingGroupReport(alertingGroup);
                    provisioningReports.Add(report);
                }

                Console.WriteLine("Finished Reading Reports");

                return provisioningReports;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                throw;
            }
        }

        private async Task<ProvisioningReport> GenerateAlertingGroupReport(AlertingGroup alertingGroup)
        {
            Console.WriteLine($"Generating report {alertingGroup.Name}");
            var provisioningReport = new ProvisioningReport
            {
                Name = alertingGroup.Name,
                Targets = alertingGroup.ReportTargets
            };
            foreach (var tableConfig in alertingGroup.DynamoDb.Tables)
            {
                var tableRows = await GenerateTableReport(tableConfig);
                provisioningReport.Rows.AddRange(tableRows);
            }
            return provisioningReport;
        }

        private async Task<IEnumerable<ProvisioningReportRow>> GenerateTableReport(Table tableConfig)
        {
            var reportRows = new List<ProvisioningReportRow>();
            Console.WriteLine($"Reading info about {tableConfig.Name}");

            var tableResource = await _tableSource.GetResourceAsync(tableConfig.Name);
            var table = tableResource.Resource;

            try
            {
                Console.WriteLine("Reading usage data");
                reportRows.Add(GetBaseTableReport(table));
                reportRows.AddRange(table.GlobalSecondaryIndexes.Select(index => GetIndexReport(table, index)));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error when reading to generate report for {tableConfig.Name}");
                Console.Error.WriteLine(ex.ToString());
            }
            return reportRows;
        }

        private ProvisioningReportRow GetBaseTableReport(TableDescription table)
        {
            return new ProvisioningReportRow
            {
                TableName = table.TableName,
                ProvisionedReadCapacityUnits = table.ProvisionedThroughput.ReadCapacityUnits,
                ProvisionedWriteCapacityUnits = table.ProvisionedThroughput.WriteCapacityUnits,
                MaxConsumedReadPerMinute = GetConsumptionMetric(table.TableName, AwsMetrics.ConsumedReadCapacity),
                MaxConsumedWritePerMinute = GetConsumptionMetric(table.TableName, AwsMetrics.ConsumedWriteCapacity )
            };
        }

        private ProvisioningReportRow GetIndexReport(TableDescription table, GlobalSecondaryIndexDescription index)
        {
            return new ProvisioningReportRow
            {
                TableName = table.TableName,
                IndexName = index.IndexName,
                ProvisionedReadCapacityUnits = index.ProvisionedThroughput.ReadCapacityUnits,
                ProvisionedWriteCapacityUnits = index.ProvisionedThroughput.WriteCapacityUnits,
                MaxConsumedReadPerMinute = GetConsumptionMetric(table.TableName, AwsMetrics.ConsumedReadCapacity, index.IndexName),
                MaxConsumedWritePerMinute = GetConsumptionMetric(table.TableName, AwsMetrics.ConsumedWriteCapacity, index.IndexName)
            };
        }

        private double GetConsumptionMetric(string tableName, string metricName, string indexName = null)
        {
            var getMetricsRequest = new GetMetricStatisticsRequest
            {
                Dimensions = new List<Dimension>
                {
                    new Dimension {Name = "TableName", Value = tableName}
                },
                Namespace = AwsNamespace.DynamoDb,
                MetricName = metricName,
                Period = 60,
                Statistics = new List<string> {"Sum"},
                Unit = StandardUnit.Count
            };

            if (!string.IsNullOrEmpty(indexName))
            {
                getMetricsRequest.Dimensions.Add(new Dimension { Name = "GlobalSecondaryIndexName", Value = indexName });
            }

            var allDataPoints = new List<Datapoint>();
            for (var i = 0; i <= ReportDuration; i++)
            {
                getMetricsRequest.StartTime = DateTime.UtcNow.AddDays(-(i + 1));
                getMetricsRequest.EndTime = DateTime.UtcNow.AddDays(-i);
                allDataPoints.AddRange(_cloudwatch.GetMetricStatistics(getMetricsRequest).Datapoints);
            }
            return allDataPoints.Count > 0 ? allDataPoints.Max(x => x.Sum) : 0;
        }
    }
}
