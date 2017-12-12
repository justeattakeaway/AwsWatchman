using System.Threading.Tasks;
using Watchman.Configuration;

namespace Watchman.Engine.Generation.Sqs
{
    public interface IOrphanQueuesReporter
    {
        Task FindAndReport(WatchmanConfiguration config);
    }
}