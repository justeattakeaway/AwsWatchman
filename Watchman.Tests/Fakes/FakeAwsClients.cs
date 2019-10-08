using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Amazon.DAX;
using Amazon.DAX.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancing.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.RDS;
using Amazon.RDS.Model;
using Moq;
using DescribeLoadBalancersResponse = Amazon.ElasticLoadBalancing.Model.DescribeLoadBalancersResponse;

namespace Watchman.Tests.Fakes
{
    internal static class FakeAwsClients
    {

        public static void DescribeReturnsLoadBalancers(this Mock<IAmazonElasticLoadBalancing> fake,
            IEnumerable<LoadBalancerDescription> loadBalancers)
        {
            fake.Setup(x => x.DescribeLoadBalancersAsync(
                    It.Is<Amazon.ElasticLoadBalancing.Model.DescribeLoadBalancersRequest>(req => req.Marker == null),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(new DescribeLoadBalancersResponse()
                {
                    LoadBalancerDescriptions = loadBalancers.ToList()
                });
        }

        public static void HasClusters(this Mock<IAmazonDAX> fake, IEnumerable<Cluster> clusters)
        {
            fake
                .Setup(l => l.DescribeClustersAsync(It.IsAny<DescribeClustersRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeClustersResponse
                {
                    Clusters = clusters.ToList()
                });
            
        }

        public static void HasDynamoTables(this Mock<IAmazonDynamoDB> fake, IEnumerable<TableDescription> tables)
        {
            tables = tables.ToList();

            fake
                .Setup(x => x.ListTablesAsync((string)null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListTablesResponse()
                {
                    TableNames = tables.Select(t => t.TableName).ToList()
                });

            foreach (var table in tables)
            {
                fake
                    .Setup(x => x.DescribeTableAsync(table.TableName, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DescribeTableResponse()
                    {
                        Table = table
                    });

            }
        }

        public static void HasCloudFrontDistributions(this Mock<IAmazonCloudFront> fake,
            IEnumerable<DistributionSummary> distributionSummaries)
        {
            fake.Setup(l => l.ListDistributionsAsync(It.IsAny<ListDistributionsRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListDistributionsResponse()
                {
                    DistributionList = new DistributionList
                    {
                        Items = distributionSummaries.ToList()
                    }
                });
        }

        public static void HasSqsQueues(this Mock<IAmazonCloudWatch> fake, IEnumerable<string> queues)
        {
            fake
                .Setup(x => x.ListMetricsAsync(It.IsAny<ListMetricsRequest>(), new CancellationToken()))
                .ReturnsAsync(new ListMetricsResponse()
                              {
                                  Metrics = queues.Select(q => new Metric()
                                                               {
                                                                   MetricName = "ApproximateAgeOfOldestMessage",
                                                                   Dimensions =
                                                                       new List<Amazon.CloudWatch.Model.Dimension
                                                                       >()
                                                                       {
                                                                           new Amazon.CloudWatch.Model.Dimension()
                                                                           {
                                                                               Name = "QueueName",
                                                                               Value = q
                                                                           }
                                                                       }
                                                               }).ToList()
                              });

        }

        public static void HasLambdaFunctions(this Mock<IAmazonLambda> fake, IEnumerable<FunctionConfiguration> functions)
        {
            fake
                .Setup(l => l.ListFunctionsAsync(It.IsAny<ListFunctionsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListFunctionsResponse()
                {
                    Functions = functions.ToList()
                });
            
        }

        public static void HasRdsClusters(this Mock<IAmazonRDS> fake, IEnumerable<DBCluster> dbClusters)
        {
            fake
                .Setup(l => l.DescribeDBClustersAsync(It.IsAny<DescribeDBClustersRequest>(), new CancellationToken()))
                .ReturnsAsync(new DescribeDBClustersResponse()
                {
                    DBClusters = dbClusters.ToList()
                });
        }

        public static void HasAutoScalingGroups(this Mock<IAmazonAutoScaling> fake, IEnumerable<AutoScalingGroup> groups)
        {
            fake
                .Setup(a => a.DescribeAutoScalingGroupsAsync(It.IsAny<DescribeAutoScalingGroupsRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeAutoScalingGroupsResponse()
                {
                    AutoScalingGroups = groups.ToList()
                });
        }
    }
}
