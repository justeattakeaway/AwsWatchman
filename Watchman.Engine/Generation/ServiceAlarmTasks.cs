using System;
using System.Linq;
using System.Threading.Tasks;
using Watchman.Configuration;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation
{
    public class ServiceAlarmTasks<T> : IServiceAlarmTasks<T> where T: class
    {
        private readonly IAlarmLogger _logger;
        private readonly ResourceNamePopulator<T> _populator;
        private readonly ServiceAlarmGenerator<T> _generator;
        private readonly OrphanResourcesReporter<T> _orphansReporter;
        private readonly Func<WatchmanConfiguration, WatchmanServiceConfiguration> _serviceConfigMapper;

        public ServiceAlarmTasks(
            IAlarmLogger logger,
            ResourceNamePopulator<T> populator,
            ServiceAlarmGenerator<T> generator,
            OrphanResourcesReporter<T> orphansReporter,
            Func<WatchmanConfiguration, WatchmanServiceConfiguration> serviceConfigMapper)
        {
            _populator = populator;
            _generator = generator;
            _orphansReporter = orphansReporter;
            _serviceConfigMapper = serviceConfigMapper;
            _logger = logger;
        }

        public async Task GenerateAlarmsForService(
        WatchmanConfiguration config, RunMode mode)
        {
            var serviceConfig = _serviceConfigMapper(config);

            if (!ServiceConfigIsPopulated(serviceConfig))
            {
                _logger.Info($"No resources for {serviceConfig.ServiceName}. No action taken for this resource type");
                return;
            }

            await PopulateResourceNames(serviceConfig);
            await GenerateAlarms(serviceConfig, mode);
            await ReportOrphans(serviceConfig);
        }

        private bool ServiceConfigIsPopulated(WatchmanServiceConfiguration serviceConfig)
        {
            if (serviceConfig == null || ! serviceConfig.AlertingGroups.Any())
            {
                return false;
            }

            var resources = serviceConfig.AlertingGroups.SelectMany(ag => ag.Service?.Resources);

            return resources.Any();
        }

        private async Task PopulateResourceNames(WatchmanServiceConfiguration serviceConfig)
        {
            foreach (var group in serviceConfig.AlertingGroups)
            {
                await _populator.PopulateResourceNames(group);
            }
        }

        private Task GenerateAlarms(WatchmanServiceConfiguration serviceConfig, RunMode mode)
        {
            return _generator.GenerateAlarmsFor(serviceConfig, mode);
        }

        private Task ReportOrphans(WatchmanServiceConfiguration serviceConfig)
        {
            return _orphansReporter.FindAndReport(serviceConfig.ServiceName, serviceConfig.AlertingGroups);
        }
    }
}
