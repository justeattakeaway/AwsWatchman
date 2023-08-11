using Amazon.CloudFront;
using Amazon.CloudFront.Model;
using NSubstitute;
using NUnit.Framework;
using Watchman.AwsResources.Services.CloudFront;

namespace Watchman.AwsResources.Tests.Services.CloudFront
{
    [TestFixture]
    public class CloudFrontSourceTests
    {
        private CloudFrontSource _source;
        private ListDistributionsResponse _firstPage;
        private ListDistributionsResponse _secondPage;
        private ListDistributionsResponse _thirdPage;

        [SetUp]
        public void SetUp()
        {
            var stepCloudFrontMock = Substitute.For<IAmazonCloudFront>();

            _firstPage = BuildListResponse(1);
            _secondPage = BuildListResponse(2);
            _thirdPage = BuildListResponse(null);

            stepCloudFrontMock.ListDistributionsAsync(
                    Arg.Is<ListDistributionsRequest>(r => r.Marker == null),
                    Arg.Any<CancellationToken>())
                .Returns(_firstPage);

            stepCloudFrontMock.ListDistributionsAsync(
                    Arg.Is<ListDistributionsRequest>(r => r.Marker == "token-1"),
                    Arg.Any<CancellationToken>())
                .Returns(_secondPage);

            stepCloudFrontMock.ListDistributionsAsync(
                    Arg.Is<ListDistributionsRequest>(r => r.Marker == "token-2"),
                    Arg.Any<CancellationToken>())
                .Returns(_thirdPage);

            _source = new CloudFrontSource(stepCloudFrontMock);
        }

        [Test]
        public async Task GetResourceNamesAsync_MultiplePages_AllFetchedAndReturned()
        {
            var result = await _source.GetResourceNamesAsync();

            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result.First(), Is.EqualTo(_firstPage.DistributionList.Items.First().Id));
            Assert.That(result.Skip(1).First(), Is.EqualTo(_secondPage.DistributionList.Items.First().Id));
            Assert.That(result.Skip(2).First(), Is.EqualTo(_thirdPage.DistributionList.Items.First().Id));
        }

        [Test]
        public async Task GetResourceNamesAsync_SinglePage_FetchedAndReturned()
        {
            // arrange
            _firstPage.DistributionList.NextMarker = null;

            // act
            var result = await _source.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First(), Is.EqualTo(_firstPage.DistributionList.Items.First().Id));
        }

        [Test]
        public async Task GetResourceNamesAsync_EmptyResult_EmptyListReturned()
        {
            // arrange
            _firstPage.DistributionList.NextMarker = null;
            _firstPage.DistributionList.Items = new List<DistributionSummary>();

            // act
            var result = await _source.GetResourceNamesAsync();

            // assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetResourceAsync_ReturnsCorrectResource()
        {
            // arrange
            var distributionId = _secondPage.DistributionList.Items.Single().Id;

            // act
            var result = await _source.GetResourceAsync(distributionId);

            // assert
            Assert.That(result.Id, Is.EqualTo(distributionId));
            Assert.That(result, Is.InstanceOf<DistributionSummary>());
        }

        private ListDistributionsResponse BuildListResponse(int? id)
        {
            return new ListDistributionsResponse()
            {
                DistributionList = new DistributionList
                {
                    NextMarker = id.HasValue ? $"token-{id}" : null,
                    Items = new List<DistributionSummary>
                    {
                        new DistributionSummary
                        {
                            Id = $"dist{id}Id"
                        }
                    }
                }
            };
        }
    }
}
