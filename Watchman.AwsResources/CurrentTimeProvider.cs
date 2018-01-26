using System;

namespace Watchman.AwsResources
{
    public class CurrentTimeProvider : ICurrentTimeProvider
    {
        public DateTime Now => DateTime.Now;
    }
}
