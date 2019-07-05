using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Watchman.AwsResources.Services.Sqs
{
    public class QueueDataV2Source : ResourceSourceBase<QueueDataV2>
    {
        private readonly QueueSource _queueSource;

        public QueueDataV2Source(QueueSource queueSource)
        {
            _queueSource = queueSource;
        }

        private Task<IList<string>> ReadActiveQueueNames()
        {
           return _queueSource.GetResourceNamesAsync();
        }

        protected override string GetResourceName(QueueDataV2 resource)
        {
            return resource.Name;
        }

        protected override async Task<IEnumerable<QueueDataV2>> FetchResources()
        {
            var names = await ReadActiveQueueNames();
            var processedQueues = new List<string>();

            var result = new List<QueueDataV2>();

            var mainQueues = names.Where(e => !IsErrorQueue(e)).ToArray();
            var errorQueues = names.Where(e => IsErrorQueue(e)).ToArray();

            foreach (var mainQueueName in mainQueues)
            {
                var errorQueueName = errorQueues.FirstOrDefault(e => e.StartsWith(mainQueueName, StringComparison.OrdinalIgnoreCase));

                // we could have a scenario where nothing has been published/read to/from the error queue for
                // a few weeks so CloudWatch stops reporting it. In this case we still do want to alert on the queue
                // so in this case we can guess the name as it should be predictable anyway.
                errorQueueName = errorQueueName ?? $"{mainQueueName}{ErrorQueueSuffix}";

                result.Add(new QueueDataV2 {Name = mainQueueName, ErrorQueue = new QueueDataV2 {Name = errorQueueName}});

                processedQueues.Add(mainQueueName);
                processedQueues.Add(errorQueueName);
            }

            var unprocessedQueues = names.Except(processedQueues);
            foreach (var queueName in unprocessedQueues)
            {
                result.Add(new QueueDataV2 {Name = queueName});
            }

            return result;
        }

        private static bool IsErrorQueue(string queueName)
        {
            return queueName.EndsWith(ErrorQueueSuffix, StringComparison.OrdinalIgnoreCase);
        }

        private const string ErrorQueueSuffix = "_error";
    }
}
