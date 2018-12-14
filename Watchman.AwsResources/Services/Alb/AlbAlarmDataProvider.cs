using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Amazon.ElasticLoadBalancing.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.Alb
{
    public class AlbAlarmDataProvider : IAlarmDimensionProvider<LoadBalancerDescription>,
        IResourceAttributesProvider<LoadBalancerDescription, ResourceConfig>
    {
        public List<Dimension> GetDimensions(LoadBalancerDescription resource, IList<string> dimensionNames)
        {
            return dimensionNames
                .Select(d => GetDimension(resource, d))
                .ToList();
        }

        private Dimension GetDimension(LoadBalancerDescription resource, string dimensionName)
        {
            var dim = new Dimension
            {
                Name = dimensionName
            };

            switch (dimensionName)
            {
                case "LoadBalancerName":
                    dim.Value = resource.LoadBalancerName;
                    break;

                default:
                    throw new Exception("Unsupported dimension " + dimensionName);
            }

            return dim;
        }

        public Task<decimal> GetValue(LoadBalancerDescription resource, ResourceConfig config, string property)
        {
            throw new NotImplementedException();
        }
    }
}
