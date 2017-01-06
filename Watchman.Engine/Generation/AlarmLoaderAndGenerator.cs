using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Watchman.Configuration;
using Watchman.Configuration.Load;
using Watchman.Configuration.Validation;
using Watchman.Engine.Generation.Dynamo;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Generation.Sqs;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation
{
    public class AlarmLoaderAndGenerator
    {
        private readonly IAlarmLogger _logger;
        private readonly IConfigLoader _configLoader;
        private readonly DynamoAlarmGenerator _dynamoGenerator;
        private readonly OrphanTablesReporter _orphanTablesReporter;
        private readonly SqsAlarmGenerator _sqsGenerator;
        private readonly OrphanQueuesReporter _orphanQueuesReporter;

        private readonly IAlarmCreator _creator;

        private readonly IEnumerable<IServiceAlarmTasks> _otherServices;

        public AlarmLoaderAndGenerator(
            IAlarmLogger logger,
            IConfigLoader configLoader,
            DynamoAlarmGenerator dynamoGenerator,
            OrphanTablesReporter orphanTablesReporter,
            SqsAlarmGenerator sqsGenerator,
            OrphanQueuesReporter orphanQueuesReporter,
            IAlarmCreator creator,
            IEnumerable<IServiceAlarmTasks> otherServices)
        {
            _logger = logger;
            _configLoader = configLoader;
            _dynamoGenerator = dynamoGenerator;
            _orphanTablesReporter = orphanTablesReporter;
            _sqsGenerator = sqsGenerator;
            _orphanQueuesReporter = orphanQueuesReporter;
            _creator = creator;
            _otherServices = otherServices;
        }

        public async Task LoadAndGenerateAlarms(RunMode mode)
        {
            try
            {
                _logger.Info($"Starting {mode}");

                var config = _configLoader.LoadConfig();
                ConfigValidator.Validate(config);

                if (mode == RunMode.GenerateAlarms || mode == RunMode.DryRun)
                {
                    await GenerateAlarms(config, mode);
                }

                _logger.Detail("Done");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error in run");
                throw;
            }
        }

        private async Task GenerateAlarms(WatchmanConfiguration config, RunMode mode)
        {
            await _dynamoGenerator.GenerateAlarmsFor(config, mode);
            await _orphanTablesReporter.FindAndReport(config);

            await _sqsGenerator.GenerateAlarmsFor(config, mode);
            await _orphanQueuesReporter.FindAndReport(config);

            foreach (var service in _otherServices)
            {
                await service.GenerateAlarmsForService(config, mode);
            }

            if (mode == RunMode.DryRun || mode == RunMode.GenerateAlarms)
            {
                await _creator.SaveChanges(mode == RunMode.DryRun);
            }
        }
    }
}
