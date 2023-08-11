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
using NSubstitute;
using DescribeLoadBalancersResponse = Amazon.ElasticLoadBalancing.Model.DescribeLoadBalancersResponse;

namespace Watchman.Tests.Fakes
{
    internal static class FakeAwsClients
    {

        public static void DescribeReturnsLoadBalancers(this IAmazonElasticLoadBalancing fake,
            IEnumerable<LoadBalancerDescription> loadBalancers)
        {
            fake.DescribeLoadBalancersAsync(
                    Arg.Is<Amazon.ElasticLoadBalancing.Model.DescribeLoadBalancersRequest>(req => req.Marker == null),
                    Arg.Any<CancellationToken>(
                ))
                .Returns(new DescribeLoadBalancersResponse()
                {
                    LoadBalancerDescriptions = loadBalancers.ToList()
                });
        }

        public static void HasClusters(this IAmazonDAX fake, IEnumerable<Cluster> clusters)
        {
            fake.DescribeClustersAsync(Arg.Any<DescribeClustersRequest>(), Arg.Any<CancellationToken>())
                .Returns(new DescribeClustersResponse
                {
                    Clusters = clusters.ToList()
                });

        }

        public static void HasDynamoTables(this IAmazonDynamoDB fake, IEnumerable<TableDescription> tables)
        {
            tables = tables.ToList();

            fake.ListTablesAsync((string)null, Arg.Any<CancellationToken>())
                .Returns(new ListTablesResponse()
                {
                    TableNames = tables.Select(t => t.TableName).ToList()
                });

            foreach (var table in tables)
            {
                fake.DescribeTableAsync(table.TableName, Arg.Any<CancellationToken>())
                    .Returns(new DescribeTableResponse()
                    {
                        Table = table
                    });

            }
        }

        public static void HasCloudFrontDistributions(this IAmazonCloudFront fake,
            IEnumerable<DistributionSummary> distributionSummaries)
        {
            fake.ListDistributionsAsync(Arg.Any<ListDistributionsRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new ListDistributionsResponse()
                {
                    DistributionList = new DistributionList
                    {
                        Items = distributionSummaries.ToList()
                    }
                });
        }

        public static void HasSqsQueues(this IAmazonCloudWatch fake, IEnumerable<string> queues)
        {
            fake.ListMetricsAsync(Arg.Any<ListMetricsRequest>(), new CancellationToken())
                .Returns(new ListMetricsResponse()
                              {
                                  Metrics = queues.Select(q => new Amazon.CloudWatch.Model.Metric()
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

        public static void HasLambdaFunctions(this IAmazonLambda fake, IEnumerable<FunctionConfiguration> functions)
        {
            fake.ListFunctionsAsync(Arg.Any<Amazon.Lambda.Model.ListFunctionsRequest>(), Arg.Any<CancellationToken>())
                .Returns(new Amazon.Lambda.Model.ListFunctionsResponse()
                {
                    Functions = functions.ToList()
                });
        }

        public static void HasRdsClusters(this IAmazonRDS fake, IEnumerable<DBCluster> dbClusters)
        {
            fake.DescribeDBClustersAsync(Arg.Any<DescribeDBClustersRequest>(), new CancellationToken())
                .Returns(new DescribeDBClustersResponse()
                {
                    DBClusters = dbClusters.ToList()
                });
        }

        public static void HasAutoScalingGroups(this IAmazonAutoScaling fake, IEnumerable<AutoScalingGroup> groups)
        {
            fake.DescribeAutoScalingGroupsAsync(Arg.Any<DescribeAutoScalingGroupsRequest>(),
                    Arg.Any<CancellationToken>())
                .Returns(new DescribeAutoScalingGroupsResponse()
                {
                    AutoScalingGroups = groups.ToList()
                });
        }
    }
}
