using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch.Model;
using Amazon.ElasticLoadBalancing.Model;

namespace Watchman.AwsResources.Services.Elb
{
    public class ElbAlarmDataProvider : IAlarmDimensionProvider<LoadBalancerDescription>, IResourceAttributesProvider<LoadBalancerDescription>
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

        public decimal GetValue(LoadBalancerDescription resource, string property)
        {
            throw new NotImplementedException();
        }
    }
}