using System.Collections.Generic;
using System.Threading.Tasks;
using Watchman.Configuration.Generic;

namespace Watchman.Engine.Generation
{
    public interface IResourceAlarmGenerator<TAlarmConfig> where TAlarmConfig : class, IServiceAlarmConfig<TAlarmConfig>, new()
    {
        Task<IList<Alarm>> GenerateAlarmsFor(AwsServiceAlarms<TAlarmConfig> service, IList<AlarmDefinition> defaults, string alarmSuffix);
    }
}
