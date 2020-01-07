using System;
using System.Collections.Generic;
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
        private readonly OrphanResourcesReporter<T, TAlarmConfig> _orphansReporter;
        private readonly IAlarmCreator _creator;
        private readonly IResourceAlarmGenerator<T, TAlarmConfig> _resourceAlarmGenerator;
        private readonly Func<WatchmanConfiguration, WatchmanServiceConfiguration<TAlarmConfig>> _serviceConfigMapper;

        public ServiceAlarmTasks(
            IAlarmLogger logger,
            ResourceNamePopulator<T, TAlarmConfig> populator,
            OrphanResourcesReporter<T, TAlarmConfig> orphansReporter,
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

            // hack to make sure the alarm creator knows about all groups
            foreach (var group in serviceConfig.AlertingGroups)
            {
                _creator.AddAlarms(group.GroupParameters, new List<Alarm>());
            }

            if (!ServiceConfigIsPopulated(serviceConfig))
            {
                _logger.Info($"No resources for {serviceConfig.ServiceName}. No action taken for this resource type");
                return new GenerateAlarmsResult();
            }

            var populatedServiceConfig = await PopulateResourceNames(serviceConfig);
            var failures = await GenerateAlarms(populatedServiceConfig, mode);
            await ReportOrphans(populatedServiceConfig);

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

        private async Task<PopulatedServiceConfiguration<TAlarmConfig, T>> PopulateResourceNames(WatchmanServiceConfiguration<TAlarmConfig> serviceConfig)
        {
            //TODO: maybe move some of this into the populator
            var items = new List<PopulatedServiceAlertingGroup<TAlarmConfig, T>>();
            foreach (var group in serviceConfig.AlertingGroups)
            {
                var populated = await _populator.PopulateResourceNames(group);
                items.Add(populated);
            }

            return new PopulatedServiceConfiguration<TAlarmConfig, T>(
                serviceConfig.ServiceName,
                items);
        }

        private async Task<List<string>> GenerateAlarms(PopulatedServiceConfiguration<TAlarmConfig, T> serviceConfig, RunMode mode)
        {
            var failures = new List<string>();

            foreach (var alertingGroup in serviceConfig.AlertingGroups)
            {
                try
                {
                    var alarmsForGroup = await _resourceAlarmGenerator.GenerateAlarmsFor(
                        alertingGroup.Service,
                        alertingGroup.GroupParameters);

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

        private Task ReportOrphans(PopulatedServiceConfiguration<TAlarmConfig, T> serviceConfig)
        {
            return _orphansReporter.FindAndReport(serviceConfig.ServiceName, serviceConfig.AlertingGroups);
        }
    }
}
