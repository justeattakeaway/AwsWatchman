using System.Text.RegularExpressions;
using Amazon.CloudWatch.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.Alb
{
    public class AlbAlarmDataProvider : IAlarmDimensionProvider<AlbResource>,
        IResourceAttributesProvider<AlbResource, ResourceConfig>
    {
        private const string LoadBalancerDimensionPattern = "loadbalancer/(.*)";
        private static readonly Regex LoadBalancerDimensionValueRegex = new Regex(LoadBalancerDimensionPattern);

        public List<Dimension> GetDimensions(AlbResource resource, IList<string> dimensionNames)
        {
            return dimensionNames
                .Select(d => GetDimension(resource, d))
                .ToList();
        }

        private Dimension GetDimension(AlbResource resource, string dimensionName)
        {
            var dim = new Dimension
            {
                Name = dimensionName
            };

            switch (dimensionName)
            {
                case "LoadBalancer":
                    var loadBalancer = resource.LoadBalancer;
                    if (LoadBalancerDimensionValueRegex.IsMatch(loadBalancer.LoadBalancerArn))
                    {
                        var match = LoadBalancerDimensionValueRegex.Match(loadBalancer.LoadBalancerArn);
                        dim.Value = match.Groups[1].Value;
                        break;
                    }
                    else
                    {
                        throw new Exception($"Could not find dimension value for LoadBalancer '{loadBalancer.LoadBalancerArn}'");
                    }
                default:
                    throw new Exception("Unsupported dimension " + dimensionName);
            }

            return dim;
        }

        public Task<decimal> GetValue(AlbResource resource, ResourceConfig config, string property)
        {
            throw new NotImplementedException();
        }
    }
}
