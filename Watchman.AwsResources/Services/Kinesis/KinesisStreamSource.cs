using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;

namespace Watchman.AwsResources.Services.Kinesis
{
    public class KinesisStreamSource : ResourceSourceBase<KinesisStreamData>
    {
        private readonly IAmazonCloudWatch _amazonCloudWatch;

        public KinesisStreamSource(IAmazonCloudWatch amazonCloudWatch)
        {
            _amazonCloudWatch = amazonCloudWatch;
        }

        protected override string GetResourceName(KinesisStreamData resource) => resource.Name;

        protected override async Task<IEnumerable<KinesisStreamData>> FetchResources()
        {
            var names = await ReadStreamNames();

            return names.Select(n => new KinesisStreamData() { Name = n }).ToList();
        }

        private async Task<IList<string>> ReadStreamNames()
        {
            var metrics = await ReadStreamMetrics();

            return metrics
                .SelectMany(ExtractStreamNames)
                .OrderBy(qn => qn)
                .Distinct()
                .ToList();
        }

        private async Task<List<Metric>> ReadStreamMetrics()
        {
            var metrics = new List<Metric>();
            string token = null;
            do
            {
                var request = new ListMetricsRequest
                {
                    MetricName = "GetRecords.IteratorAgeMilliseconds",
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

        private static IEnumerable<string> ExtractStreamNames(Metric metric) =>
            metric.Dimensions
                .Where(d => d.Name == "StreamName")
                .Select(d => d.Value);
    }
}
