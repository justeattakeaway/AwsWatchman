using Watchman.Configuration;

namespace Watchman.Engine.Generation.Sqs
{
    public interface ISqsAlarmGenerator
    {
        Task GenerateAlarmsFor(WatchmanConfiguration config, RunMode mode);
    }
}
