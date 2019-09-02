using System.Collections.Generic;
using System.Threading.Tasks;
using Watchman.Configuration.Generic;

namespace Watchman.Engine.Generation
{
    // TResource type argument is required because the implementations of this are specific to TResource (not necessarily TAlarmConfig)
    public interface IResourceAlarmGenerator<TResource, TAlarmConfig>
        where TAlarmConfig : class, IServiceAlarmConfig<TAlarmConfig>, new()
        where TResource : class
    {
        Task<IList<Alarm>> GenerateAlarmsFor(
            PopulatedServiceAlarms<TAlarmConfig, TResource> service,
            AlertingGroupParameters groupParameters);


    }
}
