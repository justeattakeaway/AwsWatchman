using System.Collections.Generic;

namespace Watchman.AwsResources.Services.Sqs
{
    public class QueueData
    {
        public string Name { get; set; }

        public ErrorQueueData ErrorQueue { get; set; }
    }
}
