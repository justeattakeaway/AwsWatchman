using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Watchman.AwsResources.Services.Sqs
{
    public class QueueServiceSource : ResourceSourceBase<QueueServiceData>
    {
        private readonly IAmazonCloudWatch _amazonCloudWatch;

        public QueueServiceSource(IAmazonCloudWatch amazonCloudWatch)
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

        protected override string GetResourceName(QueueServiceData resource)
        {
            return resource.Queue.Name;
        }

        protected override async Task<IEnumerable<QueueServiceData>> FetchResources()
        {
            var names = await ReadActiveQueueNames();

            var queueDatas = names.Where(e => !IsErrorQueue(e)).Select(n => new QueueServiceData()
                                                                            {
                                                                                Queue = new QueueData()
                                                                                {
                                                                                    Name = n
                                                                                },
                                                                                ErrorQueue = new QueueData()
                                                                                {
                                                                                     Name = names.FirstOrDefault(
                                                                                         e => e.StartsWith(n) &&
                                                                                              IsErrorQueue(e))
                                                                                }
                                                                                    
                                                                            });

            return queueDatas;
        }

        private bool IsErrorQueue(string queueName)
        {
            return queueName.ToLowerInvariant().EndsWith("_error");
        }
    }
}

