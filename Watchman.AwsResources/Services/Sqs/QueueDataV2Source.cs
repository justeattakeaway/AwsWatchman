using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Watchman.AwsResources.Services.Sqs
{
    public class QueueDataV2Source : ResourceSourceBase<QueueDataV2>
    {
        private readonly IAmazonCloudWatch _amazonCloudWatch;

        public QueueDataV2Source(IAmazonCloudWatch amazonCloudWatch)
        {
            _amazonCloudWatch = amazonCloudWatch;
        }

        private async Task<IList<string>> ReadActiveQueueNames()
        {
            var metrics = await ReadQueueMetrics();

            return metrics
                .SelectMany(ExtractQueueNames)
                .OrderBy(qn => qn)
                .Distinct()
                .ToList();
        }

        private async Task<List<Metric>> ReadQueueMetrics()
        {
            var metrics = new List<Metric>();
            string token = null;
            do
            {
                var request = new ListMetricsRequest
                {
                    MetricName = "ApproximateAgeOfOldestMessage",
                    NextToken = token
                };
                var response = await _amazonCloudWatch.ListMetricsAsync(request);

                if (response != null)
                {
                    token = response.NextToken;
                    metrics.AddRange(response.Metrics);
                }
                else
                {
                    token = null;
                }
            } while (token != null);

            return metrics;
        }

        private static IEnumerable<string> ExtractQueueNames(Metric metric)
        {
            return metric.Dimensions
                .Where(d => d.Name == "QueueName")
                .Select(d => d.Value);
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
