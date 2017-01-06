using System.Collections.Generic;
using System.Threading.Tasks;

namespace Watchman.AwsResources
{
    public interface IResourceSource<T> where T: class
    {
        Task<IList<string>> GetResourceNamesAsync();
        Task<AwsResource<T>> GetResourceAsync(string name);
    }
}
