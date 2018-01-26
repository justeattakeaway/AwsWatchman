using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.CloudWatch.Model;
using Amazon.RDS.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.Rds
{
    public class RdsAlarmDataProvider : IAlarmDimensionProvider<DBInstance, ResourceConfig>,
        IResourceAttributesProvider<DBInstance, ResourceConfig>
    {
        public decimal GetValue(DBInstance resource, ResourceConfig config, string property)
        {
            switch (property)
            {
                case "AllocatedStorage":
                    // alarm needs storage in bytes
                    return resource.AllocatedStorage * (long)Math.Pow(10, 9);
            }

            throw new Exception("Unsupported RDS property name");
        }

        public List<Dimension> GetDimensions(DBInstance resource, ResourceConfig config, IList<string> dimensionNames)
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
