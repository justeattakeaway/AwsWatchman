using System;

namespace Watchman.AwsResources
{
    public interface ICurrentTimeProvider
    {
        DateTime UtcNow { get; }
    }
}
