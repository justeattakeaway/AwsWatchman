using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.CloudWatch.Model;
using Amazon.RDS.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.Rds
{
    public class RdsAlarmDataProvider : IAlarmDimensionProvider<DBInstance>,
        IResourceAttributesProvider<DBInstance, ResourceConfig>
    {
        public Task<decimal> GetValue(DBInstance resource, ResourceConfig config, string property)
        {
            switch (property)
            {
                case "AllocatedStorage":
                    // alarm needs storage in bytes
                    return Task.FromResult((decimal)resource.AllocatedStorage * (long)Math.Pow(10, 9));
                case "Iops":
                    return Task.FromResult((decimal)resource.Iops);
            }

            throw new Exception("Unsupported RDS property name");
        }

        public List<Dimension> GetDimensions(DBInstance resource, IList<string> dimensionNames)
        {
            return dimensionNames
                .Select(x => GetDimension(resource, x))
                .ToList();
        }

        private Dimension GetDimension(DBInstance resource, string dimensionName)
        {
            var dim = new Dimension
            {
                Name = dimensionName
            };

            switch (dimensionName)
            {
                case "DBInstanceIdentifier":
                    dim.Value = resource.DBInstanceIdentifier;
                    break;

                default:
                    throw new Exception("Unsupported dimension " + dimensionName);
            }

            return dim;
        }
    }
}
