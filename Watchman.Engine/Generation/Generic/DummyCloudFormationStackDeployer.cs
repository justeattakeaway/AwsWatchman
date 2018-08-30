using System.IO;
using System.Threading.Tasks;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Generation.Generic
{
    public class DummyCloudFormationStackDeployer : ICloudformationStackDeployer
    {
        private readonly string _basePath;
        private readonly IAlarmLogger _logger;

        public DummyCloudFormationStackDeployer(string basePath, IAlarmLogger logger)
        {
            _basePath = basePath;
            _logger = logger;
        }

        public Task DeployStack(string name, string body, bool isDryRun, bool onlyUpdateExisting)
        {
            var path = Path.Combine(_basePath, $"{name}.json");

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(path);
            }

            _logger.Info($"Writing cloudformation file to {path}");

            File.WriteAllText(path, body);

            return Task.CompletedTask;
        }
    }
}
