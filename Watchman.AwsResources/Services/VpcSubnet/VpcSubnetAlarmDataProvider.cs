using System.Text.RegularExpressions;
using Amazon.CloudWatch.Model;
using Amazon.EC2.Model;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Services.VpcSubnet
{
    public class VpcSubnetAlarmDataProvider : IAlarmDimensionProvider<Subnet>,
        IResourceAttributesProvider<Subnet, ResourceConfig>
    {
        public List<Dimension> GetDimensions(Subnet resource, IList<string> dimensionNames)
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

        public Task<decimal> GetValue(Subnet resource, ResourceConfig config, string property)
        {
            switch (property)
            {
                case "NumberOfIpAddresses":
                    return Task.FromResult(GetNumberOfIpAddresses(resource));

                default:
                    throw new Exception("Unsuported property " + property);
            }
        }

        private static readonly Regex ReadCidrMask = new Regex(@"\d+$");

        private decimal GetNumberOfIpAddresses(Subnet subnet)
        {
            var match = ReadCidrMask.Match(subnet.CidrBlock);

            var cidrMask = int.Parse(match.Value);

            return (long) Math.Pow(2, 32 - cidrMask) - 2;
        }
    }
}
