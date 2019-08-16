using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Amazon.DAX.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.Dax
{
    public class DaxAlarmDataProvider : IAlarmDimensionProvider<Cluster>,
        IResourceAttributesProvider<Cluster, ResourceConfig>
    {
        public List<Dimension> GetDimensions(Cluster resource, IList<string> dimensionNames)
        {
            return dimensionNames
                .Select(x => GetDimension(resource, x))
                .ToList();
        }

        public Task<decimal> GetValue(Cluster resource, ResourceConfig config, string property)
        {
            throw new NotImplementedException();
        }

        private static Dimension GetDimension(Cluster resource, string dimensionName)
        {
            var dim = new Dimension
            {
                Name = dimensionName
            };

            switch (dimensionName)
            {
                case "ClusterId":
                    dim.Value = resource.ClusterName;
                    break;

                default:
                    throw new Exception("Unsupported dimension " + dimensionName);
            }

            return dim;
        }
    }
}
