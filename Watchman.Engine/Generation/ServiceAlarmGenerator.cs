using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine.Generation.Generic;
using Watchman.Engine.Sns;

namespace Watchman.Engine.Generation
{
    public class ServiceAlarmGenerator<T> where T:class
    {
        private readonly SnsCreator _snsCreator;
        private readonly IAlarmCreator _creator;
        private readonly ServiceAlarmBuilder<T> _serviceAlarmBuilder;

        public ServiceAlarmGenerator(
            SnsCreator snsCreator,
            IAlarmCreator creator,
            ServiceAlarmBuilder<T> serviceAlarmBuilder)
        {
            _snsCreator = snsCreator;
            _creator = creator;
            _serviceAlarmBuilder = serviceAlarmBuilder;
        }

        public async Task GenerateAlarmsFor(WatchmanServiceConfiguration config, RunMode mode)
        {
            var dryRun = mode == RunMode.DryRun;

            foreach (var alertingGroup in config.AlertingGroups)
            {
                var snsTopic = await _snsCreator.EnsureSnsTopic(alertingGroup, dryRun);

                var alarms = await _serviceAlarmBuilder.GenerateAlarmsFor(alertingGroup, snsTopic, config.Defaults);
                foreach (var alarm in alarms)
                {
                    _creator.AddAlarm(alarm);
                }
            }
        }
    }
}
