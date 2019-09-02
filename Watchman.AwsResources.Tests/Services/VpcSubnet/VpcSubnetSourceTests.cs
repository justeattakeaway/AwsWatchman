using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.EC2;
using Amazon.EC2.Model;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources.Services.VpcSubnet;

namespace Watchman.AwsResources.Tests.Services.VpcSubnet
{
    [TestFixture]
    public class VpcSubnetSourceTests
    {
        private Mock<IAmazonEC2> _fakeEc2Client;

        [SetUp]
        public void SetUp()
        {
            _fakeEc2Client = new Mock<IAmazonEC2>();
        }

        private void DescribeSubnetsReturns(List<Subnet> subnets)
        {
            _fakeEc2Client.Setup(x => x.DescribeSubnetsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DescribeSubnetsResponse()
                {
                    Subnets = subnets
                });
        }

        [Test]
        public async Task GetResourceNamesAsync_SubnetsReturned_MapsSubnetIdToName()
        {
            // arrange
            DescribeSubnetsReturns(new List<Subnet>
            {
                new Subnet {SubnetId = "Abc123"}
            });

            var sut = new VpcSubnetSource(_fakeEc2Client.Object);

            // act
            var result = await sut.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First(), Is.EqualTo("Abc123"));
        }

        [Test]
        public async Task GetResourceAsync_SubnetExists_ReturnsWrappedSubnet()
        {
            // arrange
            var subnet = new Subnet {SubnetId = "Abc123"};
            DescribeSubnetsReturns(new List<Subnet>
            {
                subnet
            });

            var sut = new VpcSubnetSource(_fakeEc2Client.Object);

            // act
            var result = await sut.GetResourceAsync("Abc123");

            // assert
            Assert.That(result, Is.EqualTo(subnet));
        }

        [Test]
        public async Task MultipleCalls_DataLoadedOnce()
        {
            // arrange
            var subnet = new Subnet { SubnetId = "Abc123" };
            DescribeSubnetsReturns(new List<Subnet>
            {
                subnet
            });

            var sut = new VpcSubnetSource(_fakeEc2Client.Object);

            // act
            await sut.GetResourceNamesAsync();
            await sut.GetResourceAsync("Abc123");
            await sut.GetResourceAsync("Abc123");

            // assert
            _fakeEc2Client.Verify(x => x.DescribeSubnetsAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
