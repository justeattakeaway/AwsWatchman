using System.Collections.Generic;

namespace Watchman.AwsResources.Services.Sqs
{
    public class QueueData
    {
        public string Url { get; set; }

        public IDictionary<string, string> Attributes { get; set; }
    }
}
