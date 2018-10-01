using System;
using System.Collections.Generic;
using System.Text;

namespace Watchman.AwsResources.Services.Sqs
{
    public class QueueServiceData
    {
        public QueueData Queue { get; set; }


        public QueueData ErrorQueue { get; set; }
    }
}
