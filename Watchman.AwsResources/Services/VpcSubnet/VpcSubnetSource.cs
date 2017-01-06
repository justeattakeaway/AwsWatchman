using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Watchman.AwsResources.Services.VpcSubnet
{
    public class VpcSubnetSource : ResourceSourceBase<Subnet>
    {
        private readonly IAmazonEC2 _ec2Client;

        public VpcSubnetSource(IAmazonEC2 ec2Client)
        {
            _ec2Client = ec2Client;
        }

        protected override async Task<IEnumerable<Subnet>> FetchResources()
        {
            return (await _ec2Client.DescribeSubnetsAsync()).Subnets;
        }

        protected override string GetResourceName(Subnet resource)
        {
            return resource.SubnetId;
        }
    }
}
