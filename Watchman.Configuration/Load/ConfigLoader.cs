using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Watchman.Configuration.Load
{
    public class ConfigLoader : IConfigLoader
    {
        private readonly FileSettings _fileSettings;
        private readonly IConfigLoadLogger _logger;
        private readonly JsonSerializerSettings _serializationSettings;


        public ConfigLoader(FileSettings fileSettings, IConfigLoadLogger logger)
        {
            _fileSettings = fileSettings;
            _logger = logger;
            _serializationSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace,
                Converters = new List<JsonConverter> {new AlertingGroupConverter(_logger)}
            };
        }

        public WatchmanConfiguration LoadConfig()
        {
            var configFolder = _fileSettings.FolderLocation;

            if (string.IsNullOrWhiteSpace(configFolder))
            {
                throw new Exception("Must specify ConfigFolderLocation");
            }

            if (!Directory.Exists(configFolder))
            {
                throw new DirectoryNotFoundException($"Cannot find config folder {configFolder}");
            }


            var configFileNames = Directory.EnumerateFiles(configFolder)
                .Where(fileName => fileName.EndsWith(".json"))
                .ToList();

            if (configFileNames.Count == 0)
            {
                _logger.Error($"No .json files were found in folder {configFolder}");
            }

            var alertingGroups = new List<AlertingGroup>();

            foreach (var configFileName in configFileNames)
            {
                var group = LoadGroupFromFile(configFileName);
                if (group != null)
                {
                    alertingGroups.Add(group);
                }
            }

            _logger.Info($"Read {alertingGroups.Count} alerting groups from {configFileNames.Count} files");

            return new WatchmanConfiguration
            {
                AlertingGroups = alertingGroups
            };
        }

        private AlertingGroup LoadGroupFromFile(string configFileName)
        {
            try
            {
                var fileContents = File.ReadAllText(configFileName);
                var group = JsonConvert.DeserializeObject<AlertingGroup>(fileContents, _serializationSettings);

                LogAlertingGroup(configFileName, group);
                return group;
            }
            catch (Exception ex)
            {
                throw new ConfigException($"cannot read config file {configFileName}: {ex.Message}", ex);
            }
        }

        private void LogAlertingGroup(string configFileName, AlertingGroup group)
        {
            var containedServiceCounts = CountContainedServices(group);

            _logger.Info($"Read alerting group {group.Name} containing {containedServiceCounts} from file {configFileName}");

            if (group.IsCatchAll)
            {
                _logger.Detail($"Alerting group {group.Name} is catch-all");
            }

            var monitorThrottling = group.DynamoDb.MonitorThrottling ?? false;

            if (monitorThrottling)
            {
                _logger.Detail($"Alerting group {group.Name} is monitoring throttled reads and writes");
            }

            if (group.DynamoDb?.Tables != null)
            {
                foreach (var table in group.DynamoDb.Tables)
                {
                    _logger.Detail(DescribeTable(table));
                }
            }

            if (group.Sqs?.Queues != null)
            {
                foreach (var queue in group.Sqs.Queues)
                {
                    _logger.Detail(DescribeQueue(queue));
                }
            }
        }

        private static string CountContainedServices(AlertingGroup group)
        {
            var tableCount = group.DynamoDb.Tables?.Count ?? 0;
            var queueCount = group.Sqs.Queues?.Count ?? 0;
            var serviceCount = CountGenericServices(group);

            if ((tableCount == 0) && (queueCount == 0) && (serviceCount == 0))
            {
                return "nothing";
            }

            var items = new List<string>();

            if (tableCount > 0)
            {
                items.Add($"{tableCount} tables");
            }

            if (queueCount > 0)
            {
                items.Add($"{queueCount} queues");
            }

            if (serviceCount > 0)
            {
                items.Add($"{serviceCount} service definitions");
            }

            return string.Join(", ", items);
        }

        private static int CountGenericServices(AlertingGroup group)
        {
            if (group.Services == null)
            {
                return 0;
            }

            return group.Services.Values
                .Select(v => v.Resources?.Count ?? 0)
                .Sum();
        }

        private static string DescribeTable(Table table)
        {
            string description;

            if (!string.IsNullOrWhiteSpace(table.Name))
            {
                description = "Table " + table.Name;
            }
            else
            {
                description = "Table pattern " + table.Pattern;
            }

            if (table.Threshold.HasValue)
            {
                description += " at threshold " + table.Threshold.Value;
            }

            if (table.MonitorWrites.HasValue)
            {
                description += " with monitor writes " + table.MonitorWrites.Value;
            }

            return description;
        }

        private string DescribeQueue(Queue queue)
        {
            string description;

            if (!string.IsNullOrWhiteSpace(queue.Name))
            {
                description = "Queue " + queue.Name;
            }
            else
            {
                description = "Queue pattern " + queue.Pattern;
            }

            if (queue.LengthThreshold.HasValue)
            {
                description += " at queue length threshold " + queue.LengthThreshold.Value;
            }

            if (queue.Errors?.LengthThreshold != null)
            {
                description += " at error queue length threshold " + queue.Errors.LengthThreshold.Value;
            }

            return description;
        }
    }
}
