namespace Watchman.AwsResources
{
    public class AwsResource<T> : IAwsResource where T: class
    {
        public string Name { get; }

        private Func<AwsResource<T>, Task<T>> _resourceGetter;

        public AwsResource(string name, Func<AwsResource<T>, Task<T>> resourceGetter)
        {
            _resourceGetter = resourceGetter;

            Name = name;
        }

        public async Task<T> GetFullResource()
        {
            return await _resourceGetter(this);
        }
    }
}
