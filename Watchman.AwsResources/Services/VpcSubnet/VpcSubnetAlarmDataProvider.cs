using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Amazon.CloudWatch.Model;
using Amazon.EC2.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.VpcSubnet
{
    public class VpcSubnetAlarmDataProvider : IAlarmDimensionProvider<Subnet, ResourceConfig>, IResourceAttributesProvider<Subnet>
    {
        public List<Dimension> GetDimensions(Subnet resource, ResourceConfig config, IList<string> dimensionNames)
        {
            return dimensionNames.Select(d => GetDimension(resource, d)).ToList();
        }

        private Dimension GetDimension(Subnet subnet, string name)
        {
            switch (name)
            {
                case "Subnet":
                    return new Dimension {Name = "Subnet", Value = subnet.SubnetId};

                default:
                    throw new Exception("Unsuported dimension " + name);
            }
        }

        public decimal GetValue(Subnet resource, string property)
        {
            switch (property)
            {
                case "NumberOfIpAddresses":
                    return GetNumberOfIpAddresses(resource);

                default:
                    throw new Exception("Unsuported property " + property);
            }
        }

        private static readonly Regex ReadCidrMask = new Regex(@"\d+$");

        private long GetNumberOfIpAddresses(Subnet subnet)
        {
            var match = ReadCidrMask.Match(subnet.CidrBlock);

            var cidrMask = int.Parse(match.Value);

            return (long) Math.Pow(2, 32 - cidrMask) - 2;
        }
    }
}
