using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using NSubstitute;
using NUnit.Framework;
using Watchman.AwsResources.Services.Sqs;

namespace Watchman.AwsResources.Tests.Services.Sqs
{
    [TestFixture]
    public class QueueDataV2SourceTests
    {
        private ListMetricsResponse _firstPage;
        private ListMetricsResponse _secondPage;
        private ListMetricsResponse _thirdPage;

        private QueueDataV2Source SUT;

        [SetUp]
        public void Setup()
        {
            _firstPage = new ListMetricsResponse
            {
                NextToken = "token-1",
                Metrics = new List<Metric>
                {
                    new Metric
                    {
                        MetricName = "ApproximateAgeOfOldestMessage",
                        Dimensions = new List<Dimension>
                        {
                            new Dimension
                            {
                                Name = "QueueName",
                                Value = "Queue-1"
                            }
                        }
                    }
                }
            };
            _secondPage = new ListMetricsResponse
            {
                NextToken = "token-2",
                Metrics = new List<Metric>
                {
                    new Metric
                    {
                        MetricName = "ApproximateAgeOfOldestMessage",
                        Dimensions = new List<Dimension>
                        {
                            new Dimension
                            {
                                Name = "QueueName",
                                Value = "Queue-2_error"
                            }
                        }
                    }
                }
            };
            _thirdPage = new ListMetricsResponse
            {
                NextToken = "token-3",
                Metrics = new List<Metric>
                {
                    new Metric
                    {
                        MetricName = "ApproximateAgeOfOldestMessage",
                        Dimensions = new List<Dimension>
                        {
                            new Dimension
                            {
                                Name = "QueueName",
                                Value = "Queue-3"
                            }
                        }
                    },
                    new Metric
                    {
                        MetricName = "ApproximateAgeOfOldestMessage",
                        Dimensions = new List<Dimension>
                        {
                            new Dimension
                            {
                                Name = "QueueName",
                                Value = "Queue-3_error"
                            }
                        }
                    }
                }
            };

            var cloudWatchMock = Substitute.For<IAmazonCloudWatch>();
            cloudWatchMock.ListMetricsAsync(
                Arg.Is<ListMetricsRequest>(r => r.MetricName == "ApproximateAgeOfOldestMessage" && r.NextToken == null),
                Arg.Any<CancellationToken>())
                .Returns(_firstPage);

            cloudWatchMock.ListMetricsAsync(
                Arg.Is<ListMetricsRequest>(r => r.MetricName == "ApproximateAgeOfOldestMessage" && r.NextToken == "token-1"),
                Arg.Any<CancellationToken>())
                .Returns(_secondPage);

            cloudWatchMock.ListMetricsAsync(
                    Arg.Is<ListMetricsRequest>(r => r.MetricName == "ApproximateAgeOfOldestMessage" && r.NextToken == "token-2"),
                    Arg.Any<CancellationToken>())
                .Returns(_thirdPage);


            SUT = new QueueDataV2Source(new QueueSource(cloudWatchMock));
        }

        [Test]
        public async Task GetResourcesAsync_EmptyResult_EmptyListReturned()
        {
            // arrange
            _firstPage.NextToken = null;
            _firstPage.Metrics = new List<Metric>();

            // act
            var result = await SUT.GetResourceNamesAsync();

            // assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetResourcesAsync_SinglePage_FetchedAndReturned()
        {
            // arrange
            _firstPage.NextToken = null;

            // act
            var result = await SUT.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));

            Assert.That(result.First(), Is.EqualTo(GetResourceName(_firstPage)));
        }

        [Test]
        public async Task GetResourcesAsync_MultiplePages_AllFetchedAndReturned()
        {
            // act
            var result = await SUT.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result.First(), Is.EqualTo(GetResourceName(_firstPage)));
            Assert.That(result.Skip(1).First(), Is.EqualTo(GetResourceName(_secondPage)));
            Assert.That(result.Skip(2).First(), Is.EqualTo(GetResourceName(_thirdPage)));
        }

        [Test]
        public async Task GetResourceAsync_ReturnsCorrectResource_WhenOnlyWorkingQueuePresent()
        {
            // arrange
            var resourceName = GetResourceName(_firstPage);

            // act
            var result = await SUT.GetResourceAsync(resourceName);

            // assert
            Assert.That(result, Is.InstanceOf<QueueDataV2>());
            Assert.That(result.Name, Is.EqualTo(resourceName));

            Assert.That(result.WorkingQueue, Is.Not.Null);
            Assert.That(result.WorkingQueue.Name, Is.EqualTo("Queue-1"));

            Assert.That(result.ErrorQueue, Is.Not.Null);
            Assert.That(result.ErrorQueue.Name, Is.EqualTo("Queue-1_error"));
        }

        [Test]
        public async Task GetResourceAsync_ReturnsCorrectResource_WhenOnlyErrorQueuePresent()
        {
            // arrange
            var resourceName = GetResourceName(_secondPage);

            // act
            var result = await SUT.GetResourceAsync(resourceName);

            // assert
            Assert.That(result, Is.InstanceOf<QueueDataV2>());
            Assert.That(result.Name, Is.EqualTo(resourceName));

            Assert.That(result.WorkingQueue, Is.Null);

            Assert.That(result.ErrorQueue, Is.Not.Null);
            Assert.That(result.ErrorQueue.Name, Is.EqualTo("Queue-2_error"));
        }

        [Test]
        public async Task GetResourceAsync_ReturnsCorrectResource_WhenBothQueuesPresent()
        {
            // arrange
            var resourceName = GetResourceName(_thirdPage);

            // act
            var result = await SUT.GetResourceAsync(resourceName);

            // assert
            Assert.That(result, Is.InstanceOf<QueueDataV2>());
            Assert.That(result.Name, Is.EqualTo(resourceName));

            Assert.That(result.WorkingQueue, Is.Not.Null);
            Assert.That(result.WorkingQueue.Name, Is.EqualTo("Queue-3"));

            Assert.That(result.ErrorQueue, Is.Not.Null);
            Assert.That(result.ErrorQueue.Name, Is.EqualTo("Queue-3_error"));
        }

        private static string GetResourceName(ListMetricsResponse metrics)
        {
            var queueName = metrics.Metrics.SelectMany(i => i.Dimensions).First(i => i.Name == "QueueName").Value;
            return TrimEnd(queueName, "_error", StringComparison.OrdinalIgnoreCase);
        }

        // TODO: extract into extensions
        public static string TrimEnd(string input, string suffixToRemove, StringComparison comparisonType)
        {
            if (input != null && suffixToRemove != null
                && input.EndsWith(suffixToRemove, comparisonType))
            {
                return input.Substring(0, input.Length - suffixToRemove.Length);
            }

            return input;
        }
    }
}
