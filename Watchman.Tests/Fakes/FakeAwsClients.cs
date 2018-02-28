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

        public static IAmazonElasticLoadBalancing CreateElbClientForLoadBalancers(
            IEnumerable<LoadBalancerDescription> loadBalancers)
        {
            var fakeClient = new Mock<IAmazonElasticLoadBalancing>();
            fakeClient.DescribeReturnsLoadBalancers(loadBalancers);
            return fakeClient.Object;
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

        public static void HasLambdaFunctions(this Mock<IAmazonLambda> fake, IEnumerable<FunctionConfiguration> functions)
        {
            fake
                .Setup(l => l.ListFunctionsAsync(It.IsAny<ListFunctionsRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ListFunctionsResponse()
                {
                    Functions = functions.ToList()
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
