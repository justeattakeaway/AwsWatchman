using Amazon.RDS;
using Amazon.RDS.Model;
using NSubstitute;
using NUnit.Framework;
using Watchman.AwsResources.Services.Rds;

namespace Watchman.AwsResources.Tests.Services.Rds
{
    [TestFixture]
    public class RdsSourceTests
    {
        private DescribeDBInstancesResponse _firstPage;
        private DescribeDBInstancesResponse _secondPage;
        private DescribeDBInstancesResponse _thirdPage;

        private RdsSource _rdsSource;

        [SetUp]
        public void Setup()
        {
            _firstPage = new DescribeDBInstancesResponse
            {
                Marker = "token-1",
                DBInstances = new List<DBInstance>
                {
                    new DBInstance {DBInstanceIdentifier = "DBInstance-1"}
                }
            };
            _secondPage = new DescribeDBInstancesResponse
            {
                Marker = "token-2",
                DBInstances = new List<DBInstance>
                {
                    new DBInstance {DBInstanceIdentifier = "DBInstance-2"}
                }
            };
            _thirdPage = new DescribeDBInstancesResponse
            {
                DBInstances = new List<DBInstance>
                {
                    new DBInstance {DBInstanceIdentifier = "DBInstance-3"}
                }
            };

            var rdsMock = Substitute.For<IAmazonRDS>();
            rdsMock.DescribeDBInstancesAsync(
                Arg.Is<DescribeDBInstancesRequest>(r => r.Marker == null),
                Arg.Any<CancellationToken>())
                .Returns(_firstPage);

            rdsMock.DescribeDBInstancesAsync(
                Arg.Is<DescribeDBInstancesRequest>(r => r.Marker == "token-1"),
                Arg.Any<CancellationToken>())
                .Returns(_secondPage);

            rdsMock.DescribeDBInstancesAsync(
                Arg.Is<DescribeDBInstancesRequest>(r => r.Marker == "token-2"),
                Arg.Any<CancellationToken>())
                .Returns(_thirdPage);

            _rdsSource = new RdsSource(rdsMock);
        }

        [Test]
        public async Task GetResourcesAsync_MultiplePages_AllFetchedAndReturned()
        {
            // arrange

            // act
            var result = await _rdsSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result.First(), Is.EqualTo(_firstPage.DBInstances.Single().DBInstanceIdentifier));
            Assert.That(result.Skip(1).First(), Is.EqualTo(_secondPage.DBInstances.Single().DBInstanceIdentifier));
            Assert.That(result.Skip(2).First(), Is.EqualTo(_thirdPage.DBInstances.Single().DBInstanceIdentifier));
        }

        [Test]
        public async Task GetResourcesAsync_SinglePage_FetchedAndReturned()
        {
            // arrange
            _firstPage.Marker = null;

            // act
            var result = await _rdsSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));

            Assert.That(result.First(), Is.EqualTo(_firstPage.DBInstances.Single().DBInstanceIdentifier));
        }

        [Test]
        public async Task GetResourcesAsync_EmptyResult_EmptyListReturned()
        {
            // arrange
            _firstPage.Marker = null;
            _firstPage.DBInstances = new List<DBInstance>();

            // act
            var result = await _rdsSource.GetResourceNamesAsync();

            // assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetResouceAsync_ReturnsCorrectResource()
        {
            // arrange
            var secondDbInstanceName = _secondPage.DBInstances.First().DBInstanceIdentifier;

            // act
            var result = await _rdsSource.GetResourceAsync(secondDbInstanceName);

            // assert
            Assert.That(result, Is.InstanceOf<DBInstance>());
            Assert.That(result.DBInstanceIdentifier, Is.EqualTo(secondDbInstanceName));
        }
    }
}
