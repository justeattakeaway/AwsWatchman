using Amazon.EC2.Model;
using NUnit.Framework;
using Watchman.AwsResources.Services.VpcSubnet;
using Watchman.Configuration.Generic;

namespace Watchman.AwsResources.Tests.Services.VpcSubnet
{
    [TestFixture]
    public class VpcSubnetAlarmDataProviderTests
    {
        [Test]
        public void GetDimensions_ValidDimensionNames_GeneratedCorrectly()
        {
            // arrange
            var sut = new VpcSubnetAlarmDataProvider();

            // act
            var result = sut.GetDimensions(new Subnet
            {
                SubnetId = "Abcd"
            }, new List<string> {"Subnet"});

            // assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First().Name, Is.EqualTo("Subnet"));
            Assert.That(result.First().Value, Is.EqualTo("Abcd"));
        }

        [TestCase("10.10.10.10/23", ExpectedResult = 510d)]
        [TestCase("10.10.10.10/19", ExpectedResult = 8190d)]
        public async Task<decimal> GetValue_NumberOfIpAddresses_CalculatesSizeCorrectly(string cidrBlock)
        {
            // arrange
            var sut = new VpcSubnetAlarmDataProvider();
            var subnet = new Subnet
            {
                CidrBlock = cidrBlock
            };

            // act
            var result = await sut.GetValue(subnet, new ResourceConfig(), "NumberOfIpAddresses");

            return result;
        }
    }
}
