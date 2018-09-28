using System;
using System.Collections.Generic;
using System.Text;

namespace Watchman.AwsResources.Services.Sqs
{
    public class ErrorQueueData : IAwsResource
    {
        public string Name { get; set; }
    }
}
