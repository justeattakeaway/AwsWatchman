using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Amazon.RDS.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.RdsCluster
{
    public class RdsClusterAlarmDataProvider : IAlarmDimensionProvider<DBCluster>,
        IResourceAttributesProvider<DBCluster, ResourceConfig>
    {
        public Task<decimal> GetValue(DBCluster resource, ResourceConfig config, string property)
        {
            switch (property)
            {
                case "AllocatedStorage":
                    // alarm needs storage in bytes
                    return Task.FromResult((decimal)resource.AllocatedStorage * (long)Math.Pow(10, 9));
            }

            throw new Exception("Unsupported RDSCluster property name");
        }

        public List<Dimension> GetDimensions(DBCluster resource, IList<string> dimensionNames)
        {
            return dimensionNames
                .Select(x => GetDimension(resource, x))
                .ToList();
        }

        private Dimension GetDimension(DBCluster resource, string dimensionName)
        {
            var dim = new Dimension
            {
                Name = dimensionName
            };

            switch (dimensionName)
            {
                case "DBClusterIdentifier":
                    dim.Value = resource.DBClusterIdentifier;
                    break;

                default:
                    throw new Exception("Unsupported dimension " + dimensionName);
            }

            return dim;
        }
    }
}
