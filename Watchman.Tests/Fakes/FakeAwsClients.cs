using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancing.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Moq;
using DescribeLoadBalancersResponse = Amazon.ElasticLoadBalancing.Model.DescribeLoadBalancersResponse;

namespace Watchman.Tests.Fakes
{
    internal static class FakeAwsClients
    {
        public static IAmazonElasticLoadBalancing CreateElbClientForLoadBalancers(
            IEnumerable<LoadBalancerDescription> loadBalancers)
        {
            var fakeClient = new Mock<IAmazonElasticLoadBalancing>();
            fakeClient.Setup(x => x.DescribeLoadBalancersAsync(
                    It.Is<Amazon.ElasticLoadBalancing.Model.DescribeLoadBalancersRequest>(req => req.Marker == null),
                    It.IsAny<CancellationToken>()
                ))
                .ReturnsAsync(new DescribeLoadBalancersResponse()
                {
                    LoadBalancerDescriptions = loadBalancers.ToList()
                });

            return fakeClient.Object;
        }

        public static IAmazonDynamoDB CreateDynamoClientForTables(IEnumerable<TableDescription> tables)
        {
            tables = tables.ToList();

            var fakeDynamo = new Mock<IAmazonDynamoDB>();

            fakeDynamo
                .Setup(x => x.ListTablesAsync((string)null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListTablesResponse()
                {
                    TableNames = tables.Select(t => t.TableName).ToList()
                });

            foreach (var table in tables)
            {
                fakeDynamo
                    .Setup(x => x.DescribeTableAsync(table.TableName, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new DescribeTableResponse()
                    {
                        Table = table
                    });

            }

            return fakeDynamo.Object;
        }

        public static IAmazonLambda CreateLambdaClientForFunctions(IEnumerable<FunctionConfiguration> functions)
        {
            var fakeLambda = new Mock<IAmazonLambda>();
            fakeLambda
                .Setup(l => l.ListFunctionsAsync(It.IsAny<ListFunctionsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListFunctionsResponse()
                {
                    Functions = functions.ToList()
                });

            return fakeLambda.Object;
        }

        public static IAmazonAutoScaling CreateAutoScalingClientForGroups(IEnumerable<AutoScalingGroup> groups)
        {
            var fake = new Mock<IAmazonAutoScaling>();
            fake
                .Setup(a => a.DescribeAutoScalingGroupsAsync(It.IsAny<DescribeAutoScalingGroupsRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeAutoScalingGroupsResponse()
                {
                    AutoScalingGroups = groups.ToList()
                });

            return fake.Object;
        }
    }
}
