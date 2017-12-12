using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Watchman.Engine.Generation.Generic;

namespace Watchman.Tests.Fakes
{
    public class FakeStackDeployer : ICloudformationStackDeployer
    {
        private readonly Dictionary<string, string> _submitted = new Dictionary<string, string>();
        public Task DeployStack(string name, string body, bool isDryRun)
        {
            if (!isDryRun)
            {
                _submitted.Add(name, body);
            }

            return Task.CompletedTask;
        }

        public bool StackWasDeployed(string name) => _submitted.ContainsKey(name);

        public string StackJson(string name) => _submitted[name];

        public Template Stack(string name) => JsonConvert.DeserializeObject<Template>(_submitted[name]);
    }
}
