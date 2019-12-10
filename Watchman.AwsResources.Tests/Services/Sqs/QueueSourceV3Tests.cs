using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources.Services.Sqs;
using Watchman.AwsResources.Services.Sqs.V3;

namespace Watchman.AwsResources.Tests.Services.Sqs
{
    [TestFixture]
    public class QueueSourceV3Tests
    {
        private ListMetricsResponse _firstPage;
        private ListMetricsResponse _secondPage;
        private ListMetricsResponse _thirdPage;

        private QueueDataSourceV3 SUT;

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

            var cloudWatchMock = new Mock<IAmazonCloudWatch>();
            cloudWatchMock.Setup(s => s.ListMetricsAsync(
                It.Is<ListMetricsRequest>(r => r.MetricName == "ApproximateAgeOfOldestMessage" && r.NextToken == null),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(_firstPage);

            cloudWatchMock.Setup(s => s.ListMetricsAsync(
                It.Is<ListMetricsRequest>(r => r.MetricName == "ApproximateAgeOfOldestMessage" && r.NextToken == "token-1"),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(_secondPage);

            cloudWatchMock.Setup(s => s.ListMetricsAsync(
                    It.Is<ListMetricsRequest>(r => r.MetricName == "ApproximateAgeOfOldestMessage" && r.NextToken == "token-2"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(_thirdPage);


            SUT = new QueueDataSourceV3(new QueueSource(cloudWatchMock.Object));
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
            Assert.That(result, Is.InstanceOf<QueueDataV3>());
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
            Assert.That(result, Is.InstanceOf<QueueDataV3>());
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
            Assert.That(result, Is.InstanceOf<QueueDataV3>());
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
