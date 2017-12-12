using System.Threading.Tasks;
using Watchman.Configuration;

namespace Watchman.Engine.Generation.Dynamo
{
    public interface IDynamoAlarmGenerator
    {
        Task GenerateAlarmsFor(WatchmanConfiguration config, RunMode mode);
    }
}