using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Watchman.AwsResources
{
    /// <summary>
    /// Base ResourceSource which can be used for AWS types where the model and name can be retrieved sensibly in the same call
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ResourceSourceBase<T> : IResourceSource<T> where T : class
    {
        private IList<AwsResource<T>> _resources;

        protected abstract string GetResourceName(T resource);

        protected abstract Task<IEnumerable<T>> FetchResources();

        private async Task Load()
        {
            var results = await FetchResources();
            _resources = results
                .Select(x => new AwsResource<T>(GetResourceName(x), x))
                .ToList();
        }

        public async Task<IList<string>> GetResourceNamesAsync()
        {
            return (await GetResourcesAsync()).Select(r => r.Name).ToList();
        }

        private async Task<IList<AwsResource<T>>> GetResourcesAsync()
        {
            if (_resources != null)
            {
                return _resources;
            }

            await Load();

            return _resources;
        }

        public async Task<AwsResource<T>> GetResourceAsync(string name)
        {
            var resources = await GetResourcesAsync();

            return resources.FirstOrDefault(x => x.Name == name);
        }
    }
}
