using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation.Generic
{
    public class OrphansLogger
    {
        private readonly IAlarmLogger _logger;

        public OrphansLogger(IAlarmLogger logger)
        {
            _logger = logger;
        }

        public void Log(OrphansModel orphans)
        {
            if (orphans.Items.Count == 0)
            {
                _logger.Info($"All {orphans.ServiceName} resources are monitored!");
            }
            else
            {
                _logger.Info($"{orphans.Items.Count} {orphans.ServiceName} resources are unmonitored");
            }
        }

    }
}
