using System.Threading.Tasks;

namespace Watchman.Engine.Sns
{
    public interface ISnsTopicCreator
    {
        Task<string> EnsureSnsTopic(string alertingGroupName, bool dryRun);
    }
}
