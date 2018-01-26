namespace Watchman.AwsResources
{
    public interface IResourceAttributesProvider<in T, in TAlarmConfig> where TAlarmConfig : class
    {
        decimal GetValue(T resource, TAlarmConfig config, string property);
    }
}
