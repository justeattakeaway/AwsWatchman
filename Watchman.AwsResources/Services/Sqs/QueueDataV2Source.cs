using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

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

            var queues = names
                .Where(e => !IsErrorQueue(e))
                .Select(n =>
                {
                    var errorQueueName = names.FirstOrDefault(
                        e => e.StartsWith(n) &&
                             IsErrorQueue(e));


                    // we could have a scenario where nothing has been published/read to/from the error queue for
                    // a few weeks so CloudWatch stops reporting it. In this case we still do want to alert on the queue
                    // so in this case we can guess the name as it should be predictable anyway.
                    errorQueueName = errorQueueName ?? $"{n}{ErrorQueueSuffix}";

                    return new QueueDataV2()
                    {
                        Name = n,

                        ErrorQueue = new QueueDataV2()
                        {
                            Name = errorQueueName
                        }
                    };
                });

            return queues;
        }

        private bool IsErrorQueue(string queueName)
        {
            return queueName.ToLowerInvariant().EndsWith(ErrorQueueSuffix);
        }

        private const string ErrorQueueSuffix = "_error";
    }
}
