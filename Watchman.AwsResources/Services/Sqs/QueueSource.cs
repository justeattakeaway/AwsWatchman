using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Watchman.AwsResources.Services.Sqs
{
    /// <summary>
    /// read all queues that are "active"
    /// i.e. have metrics in cloudwatch
    /// See http://stackoverflow.com/a/40346581/5599
    /// </summary>
    public class QueueSource : ResourceSourceBase<QueueData>
    {
        private readonly IAmazonCloudWatch _amazonCloudWatch;

        public QueueSource(IAmazonCloudWatch amazonCloudWatch)
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

        protected override string GetResourceName(QueueData resource)
        {
            return resource.Name;
        }

        protected override async Task<IEnumerable<QueueData>> FetchResources()
        {
            var names = await ReadActiveQueueNames();

            return names.Select(n => new QueueData() {Name = n}).ToList();
        }
    }
}
