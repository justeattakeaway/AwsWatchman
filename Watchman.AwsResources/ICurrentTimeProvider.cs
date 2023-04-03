namespace Watchman.AwsResources
{
    public interface ICurrentTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
