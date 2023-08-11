using Amazon.EC2;
using Amazon.EC2.Model;
using NSubstitute;
using NUnit.Framework;
using Watchman.AwsResources.Services.VpcSubnet;

namespace Watchman.AwsResources.Tests.Services.VpcSubnet
{
    [TestFixture]
    public class VpcSubnetSourceTests
    {
        private IAmazonEC2 _fakeEc2Client;

        [SetUp]
        public void SetUp()
        {
            _fakeEc2Client = Substitute.For<IAmazonEC2>();
        }

        private void DescribeSubnetsReturns(List<Subnet> subnets)
        {
            _fakeEc2Client.DescribeSubnetsAsync(Arg.Any<CancellationToken>())
                .Returns(new DescribeSubnetsResponse()
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

            var sut = new VpcSubnetSource(_fakeEc2Client);

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

            var sut = new VpcSubnetSource(_fakeEc2Client);

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

            var sut = new VpcSubnetSource(_fakeEc2Client);

            // act
            await sut.GetResourceNamesAsync();
            await sut.GetResourceAsync("Abc123");
            await sut.GetResourceAsync("Abc123");

            // assert
            await _fakeEc2Client.Received().DescribeSubnetsAsync(Arg.Any<CancellationToken>());
        }
    }
}
