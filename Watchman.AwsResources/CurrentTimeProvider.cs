namespace Watchman.AwsResources
{
    public class CurrentTimeProvider : ICurrentTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
