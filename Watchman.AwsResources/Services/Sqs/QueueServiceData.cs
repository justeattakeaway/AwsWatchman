using System;
using System.Collections.Generic;
using System.Text;

namespace Watchman.AwsResources.Services.Sqs
{

    public class QueueDataV2
    {
        public string Name { get; set; }

        public QueueDataV2 ErrorQueue { get; set; }

    }

}
