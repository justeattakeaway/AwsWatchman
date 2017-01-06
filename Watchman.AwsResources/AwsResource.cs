namespace Watchman.AwsResources
{
    public class AwsResource<T> : IAwsResource where T: class
    {
        public string Name { get; }

        public T Resource { get; }

        public AwsResource(string name, T resource)
        {
            Resource = resource;
            Name = name;
        }
    }
}
