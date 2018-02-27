using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        private readonly IResourceAlarmGenerator<T, TAlarmConfig> _resourceAlarmGenerator;
        private readonly Func<WatchmanConfiguration, WatchmanServiceConfiguration<TAlarmConfig>> _serviceConfigMapper;

        public ServiceAlarmTasks(
            IAlarmLogger logger,
            ResourceNamePopulator<T, TAlarmConfig> populator,
            OrphanResourcesReporter<T> orphansReporter,
            IAlarmCreator creator,
            IResourceAlarmGenerator<T, TAlarmConfig> resourceAlarmGenerator,
            Func<WatchmanConfiguration, WatchmanServiceConfiguration<TAlarmConfig>> serviceConfigMapper)
        {
            _populator = populator;
            _orphansReporter = orphansReporter;
            _creator = creator;
            _resourceAlarmGenerator = resourceAlarmGenerator;
            _serviceConfigMapper = serviceConfigMapper;
            _logger = logger;
        }

        public async Task<GenerateAlarmsResult> GenerateAlarmsForService(
        WatchmanConfiguration config, RunMode mode)
        {
            var serviceConfig = _serviceConfigMapper(config);

            if (!ServiceConfigIsPopulated(serviceConfig))
            {
                _logger.Info($"No resources for {serviceConfig.ServiceName}. No action taken for this resource type");
                return new GenerateAlarmsResult();
            }

            await PopulateResourceNames(serviceConfig);
            var failures = await GenerateAlarms(serviceConfig, mode);
            await ReportOrphans(serviceConfig);

            return new GenerateAlarmsResult(failures);
        }

        private bool ServiceConfigIsPopulated(WatchmanServiceConfiguration<TAlarmConfig> serviceConfig)
        {
            if (serviceConfig == null || ! serviceConfig.AlertingGroups.Any())
            {
                return false;
            }

            var resources = serviceConfig.AlertingGroups
                .Where(ag => ag.Service?.Resources != null)
                .SelectMany(ag => ag.Service?.Resources);

            return resources.Any();
        }

        private async Task PopulateResourceNames(WatchmanServiceConfiguration<TAlarmConfig> serviceConfig)
        {
            foreach (var group in serviceConfig.AlertingGroups)
            {
                await _populator.PopulateResourceNames(group);
            }
        }

        private async Task<List<string>> GenerateAlarms(WatchmanServiceConfiguration<TAlarmConfig> serviceConfig, RunMode mode)
        {
            var failures = new List<string>();

            foreach (var alertingGroup in serviceConfig.AlertingGroups)
            {
                try
                {
                    var alarmsForGroup = await _resourceAlarmGenerator.GenerateAlarmsFor(
                        alertingGroup.Service,
                        serviceConfig.Defaults,
                        alertingGroup.GroupParameters.AlarmNameSuffix);

                    _creator.AddAlarms(alertingGroup.GroupParameters, alarmsForGroup);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to generate alarms for group {alertingGroup.GroupParameters.Name}");
                    failures.Add(alertingGroup.GroupParameters.Name);
                }
            }

            return failures;
        }

        private Task ReportOrphans(WatchmanServiceConfiguration<TAlarmConfig> serviceConfig)
        {
            return _orphansReporter.FindAndReport(serviceConfig.ServiceName, serviceConfig.AlertingGroups);
        }
    }
}
