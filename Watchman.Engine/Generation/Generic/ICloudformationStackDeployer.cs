using System.Threading.Tasks;

namespace Watchman.Engine.Generation.Generic
{
    public interface ICloudformationStackDeployer
    {
        Task DeployStack(string name, string body, bool isDryRun);
    }
}
