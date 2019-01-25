using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Watchman.Configuration;
using Watchman.Configuration.Load;
using Watchman.Configuration.Validation;
using Watchman.Engine.Generation.Dynamo;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Generation.Sqs;
using Watchman.Engine.LegacyTracking;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation
{
    public class AlarmLoaderAndGenerator
    {
        private readonly IAlarmLogger _logger;
        private readonly IConfigLoader _configLoader;
        private readonly IDynamoAlarmGenerator _dynamoGenerator;
        private readonly IOrphanTablesReporter _orphanTablesReporter;
        private readonly ISqsAlarmGenerator _sqsGenerator;
        private readonly IOrphanQueuesReporter _orphanQueuesReporter;
        private readonly IOrphanedAlarmReporter _orphanedAlarmReporter;
        private bool _hasRun = false;

        private readonly IAlarmCreator _creator;

        private readonly IEnumerable<IServiceAlarmTasks> _otherServices;

        public AlarmLoaderAndGenerator(
            IAlarmLogger logger,
            IConfigLoader configLoader,
            IDynamoAlarmGenerator dynamoGenerator,
            IOrphanTablesReporter orphanTablesReporter,
            ISqsAlarmGenerator sqsGenerator,
            IOrphanQueuesReporter orphanQueuesReporter,
            IOrphanedAlarmReporter orphanedAlarmReporter,
            IAlarmCreator creator,
            IEnumerable<IServiceAlarmTasks> otherServices)
        {
            _logger = logger;
            _configLoader = configLoader;
            _dynamoGenerator = dynamoGenerator;
            _orphanTablesReporter = orphanTablesReporter;
            _sqsGenerator = sqsGenerator;
            _orphanQueuesReporter = orphanQueuesReporter;
            _orphanedAlarmReporter = orphanedAlarmReporter;
            _creator = creator;
            _otherServices = otherServices;
        }

        public async Task LoadAndGenerateAlarms(RunMode mode)
        {
            if (_hasRun)
            {
                // there is loads of state etc. and you get duplicate alarms
                // shouldn't happen in real life but I discovered it in tests
                throw new InvalidOperationException($"{nameof(LoadAndGenerateAlarms)} can only be called once");
            }

            try
            {
                _hasRun = true;
                _logger.Info($"Starting {mode}");

                var config = _configLoader.LoadConfig();
                ConfigValidator.Validate(config);

                if (mode == RunMode.GenerateAlarms || mode == RunMode.DryRun)
                {
                    await GenerateAlarms(config, mode);
                }

                if (mode == RunMode.GenerateAlarms)
                {
                    await LogOrphanedAlarms();
                }

                _logger.Detail("Done");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in run");
                throw;
            }
        }

        private async Task LogOrphanedAlarms()
        {
            _logger.Info("Looking for old alarms");

            var orphans = await _orphanedAlarmReporter.FindOrphanedAlarms();
            _logger.Info(
                $"Found {orphans.Count} alarm(s) that appear to be created by AwsWatchman but are no longer managed:");

            if (orphans.Any())
            {
                foreach (var alarm in orphans)
                {
                    _logger.Info(
                        $" - {alarm.AlarmName}  (updated: {alarm.AlarmConfigurationUpdatedTimestamp:yyyy-MM-dd})");
                }
            }
        }

        private async Task GenerateAlarms(WatchmanConfiguration config, RunMode mode)
        {
            await _dynamoGenerator.GenerateAlarmsFor(config, mode);
            await _orphanTablesReporter.FindAndReport(config);

            await _sqsGenerator.GenerateAlarmsFor(config, mode);
            await _orphanQueuesReporter.FindAndReport(config);

            var failed = new List<IList<string>>();

            foreach (var service in _otherServices)
            {
                var result = await service.GenerateAlarmsForService(config, mode);

                failed.Add(result.FailingGroups);
            }

            var allFailed = failed.SelectMany(_ => _).Distinct().ToArray();

            if (mode == RunMode.DryRun || mode == RunMode.GenerateAlarms)
            {
                await _creator.SaveChanges(mode == RunMode.DryRun);
            }

            if (allFailed.Any())
            {
                throw new Exception("The following groups reported errors and were not deployed: " +
                                    $"{string.Join(", ", allFailed)}. please see logs.");
            }
        }
    }
}
