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
        private readonly IAlarmCreator _creator;
        private readonly ServiceAlarmBuilder<T> _serviceAlarmBuilder;

        public ServiceAlarmGenerator(
            IAlarmCreator creator,
            ServiceAlarmBuilder<T> serviceAlarmBuilder)
        {
            _creator = creator;
            _serviceAlarmBuilder = serviceAlarmBuilder;
        }

        public async Task GenerateAlarmsFor(WatchmanServiceConfiguration config, RunMode mode)
        {
            foreach (var alertingGroup in config.AlertingGroups)
            {
                var alarmsForGroup = await _serviceAlarmBuilder.GenerateAlarmsFor(
                    alertingGroup.Service,
                    config.Defaults,
                    alertingGroup.AlarmNameSuffix);

                _creator.AddAlarms(alertingGroup, alarmsForGroup);
            }
        }
    }
}
