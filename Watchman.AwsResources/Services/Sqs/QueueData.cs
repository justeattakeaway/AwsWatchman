using System.Collections.Generic;

namespace Watchman.AwsResources.Services.Sqs
{
    public class QueueData
    {
        public string Name { get; set; }

        public bool IsErrorQueue { get; set; }
    }
}
