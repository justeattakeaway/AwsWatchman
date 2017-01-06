using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.SQS;

namespace Watchman.AwsResources.Services.Sqs
{
    /// <summary>
    /// read all queues that are "active"
    /// i.e. have metrics in cloudwatch
    /// See http://stackoverflow.com/a/40346581/5599
    /// </summary>
    public class QueueSource : IResourceSource<QueueData>
    {
        private readonly IAmazonCloudWatch _amazonCloudWatch;
        private readonly IAmazonSQS _amazonSqs;
        private IList<string> _queueNames;

        public QueueSource(IAmazonCloudWatch amazonCloudWatch, IAmazonSQS amazonSqs)
        {
            _amazonCloudWatch = amazonCloudWatch;
            _amazonSqs = amazonSqs;
        }

        public async Task<AwsResource<QueueData>> GetResourceAsync(string name)
        {
            var attrs = await _amazonSqs.GetAttributesAsync(name);
            var queueData = new QueueData
            {
                Attributes = attrs,
                Url = name
            };

            return new AwsResource<QueueData>(name, queueData);
        }

        public async Task<IList<string>> GetResourceNamesAsync()
        {
            if (_queueNames == null)
            {
                _queueNames = await ReadActiveQueueNames();
            }

            return _queueNames;
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
            }
            while (token != null);

            return metrics;
        }

        private static IEnumerable<string> ExtractQueueNames(Metric metric)
        {
            return metric.Dimensions
                .Where(d => d.Name == "QueueName")
                .Select(d => d.Value);
        }
    }
}
