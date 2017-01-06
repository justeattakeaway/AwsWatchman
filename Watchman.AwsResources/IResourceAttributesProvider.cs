namespace Watchman.AwsResources
{
    public interface IResourceAttributesProvider<in T>
    {
        decimal GetValue(T resource, string property);
    }
}
