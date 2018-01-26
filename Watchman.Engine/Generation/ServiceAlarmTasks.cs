using System;
using System.Linq;
using System.Threading.Tasks;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation
{
    public class ServiceAlarmTasks<T, TAlarmConfig> : IServiceAlarmTasks
        where T: class
        where TAlarmConfig : class, IServiceAlarmConfig<TAlarmConfig>, new()
    {
        private readonly IAlarmLogger _logger;
        private readonly ResourceNamePopulator<T, TAlarmConfig> _populator;
        private readonly OrphanResourcesReporter<T> _orphansReporter;
        private readonly IAlarmCreator _creator;
        private readonly ServiceAlarmBuilder<T, TAlarmConfig> _serviceAlarmBuilder;
        private readonly Func<WatchmanConfiguration, WatchmanServiceConfiguration<TAlarmConfig>> _serviceConfigMapper;

        public ServiceAlarmTasks(
            IAlarmLogger logger,
            ResourceNamePopulator<T, TAlarmConfig> populator,
            OrphanResourcesReporter<T> orphansReporter,
            IAlarmCreator creator,
            ServiceAlarmBuilder<T, TAlarmConfig> serviceAlarmBuilder,
            Func<WatchmanConfiguration, WatchmanServiceConfiguration<TAlarmConfig>> serviceConfigMapper)
        {
            _populator = populator;
            _orphansReporter = orphansReporter;
            _creator = creator;
            _serviceAlarmBuilder = serviceAlarmBuilder;
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

        private bool ServiceConfigIsPopulated(WatchmanServiceConfiguration<TAlarmConfig> serviceConfig)
        {
            if (serviceConfig == null || ! serviceConfig.AlertingGroups.Any())
            {
                return false;
            }

            var resources = serviceConfig.AlertingGroups.SelectMany(ag => ag.Service?.Resources);

            return resources.Any();
        }

        private async Task PopulateResourceNames(WatchmanServiceConfiguration<TAlarmConfig> serviceConfig)
        {
            foreach (var group in serviceConfig.AlertingGroups)
            {
                await _populator.PopulateResourceNames(group);
            }
        }

        private async Task GenerateAlarms(WatchmanServiceConfiguration<TAlarmConfig> serviceConfig, RunMode mode)
        {
            foreach (var alertingGroup in serviceConfig.AlertingGroups)
            {
                var alarmsForGroup = await _serviceAlarmBuilder.GenerateAlarmsFor(
                    alertingGroup.Service,
                    serviceConfig.Defaults,
                    alertingGroup.GroupParameters.AlarmNameSuffix);

                _creator.AddAlarms(alertingGroup.GroupParameters, alarmsForGroup);
            }
        }

        private Task ReportOrphans(WatchmanServiceConfiguration<TAlarmConfig> serviceConfig)
        {
            return _orphansReporter.FindAndReport(serviceConfig.ServiceName, serviceConfig.AlertingGroups);
        }
    }
}
