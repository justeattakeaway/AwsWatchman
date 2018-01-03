using System.Threading.Tasks;
using Watchman.Configuration;

namespace Watchman.Engine.Generation.Dynamo
{
    public interface IOrphanTablesReporter
    {
        Task FindAndReport(WatchmanConfiguration config);
    }
}