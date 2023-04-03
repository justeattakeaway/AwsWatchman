namespace Watchman.AwsResources
{
    public interface IResourceSource<T> where T: class
    {
        Task<IList<AwsResource<T>>> GetResourcesAsync();
        Task<T> GetResourceAsync(string name);
        Task<IList<string>> GetResourceNamesAsync();
    }
}
