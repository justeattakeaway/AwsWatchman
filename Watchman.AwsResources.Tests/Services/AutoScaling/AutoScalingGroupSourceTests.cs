using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AutoScaling;
using Amazon.AutoScaling.Model;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources.Services.AutoScaling;

namespace Watchman.AwsResources.Tests.Services.AutoScaling
{
    [TestFixture]
    public class AutoScalingGroupSourceTests
    {
        private IAmazonAutoScaling CreateAutoScalingClientStub(params DescribeAutoScalingGroupsResponse[] pages)
        {
            var clientStub = new Mock<IAmazonAutoScaling>();

            var tokens = Enumerable
                .Range(10000, pages.Length - 1)
                .Select(x => $"Token{x}")
                .Concat(new string[] { null });

            string nextToken = null;

            foreach (var page in pages.Zip(tokens, (p, t) => new { Result = p, Token = t }))
            {
                // set next page token in result
                page.Result.NextToken = page.Token;

                // setup for current page
                string currentPageToken = nextToken;
                nextToken = page.Token;

                clientStub
                .Setup(x => x.DescribeAutoScalingGroupsAsync(
                    It.Is<DescribeAutoScalingGroupsRequest>(r => r.NextToken == currentPageToken),
                    It.IsAny<CancellationToken>())
                 )
                .Returns(Task.FromResult(page.Result));
            }

            return clientStub.Object;
        }

        [Test]
        public async Task GetResourcesAsync_MultiplePages_AllFetchedAndReturned()
        {
            // arrange
            var firstPage = new DescribeAutoScalingGroupsResponse
            {
                AutoScalingGroups = new List<AutoScalingGroup> {new AutoScalingGroup { AutoScalingGroupName = "Asg1" } }
            };

            var secondPage = new DescribeAutoScalingGroupsResponse
            {
                AutoScalingGroups = new List<AutoScalingGroup> { new AutoScalingGroup { AutoScalingGroupName = "Asg2" } }
            };

            var client = CreateAutoScalingClientStub(firstPage, secondPage);

            // act
            var sut = new AutoScalingGroupSource(client);
            var result = await sut.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result.First(), Is.EqualTo(firstPage.AutoScalingGroups.Single().AutoScalingGroupName));
            Assert.That(result.Skip(1).First(), Is.EqualTo(secondPage.AutoScalingGroups.Single().AutoScalingGroupName));
        }

        [Test]
        public async Task GetResourcesAsync_SinglePage_FetchedAndReturned()
        {
            // arrange
            var firstPage = new DescribeAutoScalingGroupsResponse
            {
                AutoScalingGroups = new List<AutoScalingGroup> { new AutoScalingGroup { AutoScalingGroupName = "Asg"} }
            };

            var client = CreateAutoScalingClientStub(firstPage);

            // act
            var sut = new AutoScalingGroupSource(client);
            var result = await sut.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result.First(), Is.EqualTo(firstPage.AutoScalingGroups.Single().AutoScalingGroupName));
        }

        [Test]
        public async Task GetResourcesAsync_EmptyResult_EmptyListReturned()
        {
            // arrange
            var firstPage = new DescribeAutoScalingGroupsResponse
            {
                AutoScalingGroups = new List<AutoScalingGroup>()
            };

            var client = CreateAutoScalingClientStub(firstPage);

            // act
            var sut = new AutoScalingGroupSource(client);
            var result = await sut.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetResouceAsync_ReturnsCorrectResource()
        {
            // arrange
            var group = new AutoScalingGroup
            {
                AutoScalingGroupName = "ASG Name"
            };

            var firstPage = new DescribeAutoScalingGroupsResponse
            {
                AutoScalingGroups = new List<AutoScalingGroup>
                {
                    group
                }
            };

            var client = CreateAutoScalingClientStub(firstPage);

            // act
            var sut = new AutoScalingGroupSource(client);
            var result = await sut.GetResourceAsync(group.AutoScalingGroupName);

            // assert
            Assert.That(result.Resource, Is.EqualTo(group));
        }
    }
}
