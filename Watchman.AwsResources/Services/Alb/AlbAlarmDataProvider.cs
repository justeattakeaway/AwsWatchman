using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Amazon.ElasticLoadBalancingV2.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.Alb
{
    public class AlbAlarmDataProvider : IAlarmDimensionProvider<LoadBalancer>,
        IResourceAttributesProvider<LoadBalancer, ResourceConfig>
    {
        private const string LoadBalancerDimensionPattern = "loadbalancer/(.*)";
        private static readonly Regex LoadBalancerDimensionValueRegex = new Regex(LoadBalancerDimensionPattern);

        public List<Dimension> GetDimensions(LoadBalancer resource, IList<string> dimensionNames)
        {
            return dimensionNames
                .Select(d => GetDimension(resource, d))
                .ToList();
        }

        private Dimension GetDimension(LoadBalancer resource, string dimensionName)
        {
            var dim = new Dimension
            {
                Name = dimensionName
            };

            switch (dimensionName)
            {
                case "LoadBalancer":
                    if (LoadBalancerDimensionValueRegex.IsMatch(resource.LoadBalancerArn))
                    {
                        var match = LoadBalancerDimensionValueRegex.Match(resource.LoadBalancerArn);
                        dim.Value = match.Groups[1].Value;
                        break;
                    }
                    else
                    {
                        throw new Exception($"Could not find dimension value for LoadBalancer with LoadBalancerArn '{resource.LoadBalancerArn}'");
                    }

                default:
                    throw new Exception("Unsupported dimension " + dimensionName);
            }

            return dim;
        }

        public Task<decimal> GetValue(LoadBalancer resource, ResourceConfig config, string property)
        {
            throw new NotImplementedException();
        }
    }
}
