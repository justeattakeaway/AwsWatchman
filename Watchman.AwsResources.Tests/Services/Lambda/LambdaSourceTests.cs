using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources.Services.Lambda;

namespace Watchman.AwsResources.Tests.Services.Lambda
{
    [TestFixture]
    public class LambdaSourceTests
    {
        private ListFunctionsResponse _firstPage;
        private ListFunctionsResponse _secondPage;
        private ListFunctionsResponse _thirdPage;

        private LambdaSource _lambdaSource;

        [SetUp]
        public void Setup()
        {
            _firstPage = new ListFunctionsResponse
            {
                NextMarker = "token-1",
                Functions = new List<FunctionConfiguration>
                {
                    new FunctionConfiguration {FunctionName = "Function-1"}
                }
            };
            _secondPage = new ListFunctionsResponse
            {
                NextMarker = "token-2",
                Functions = new List<FunctionConfiguration>
                {
                    new FunctionConfiguration {FunctionName = "Function-2"}
                }
            };
            _thirdPage = new ListFunctionsResponse
            {
                Functions = new List<FunctionConfiguration>
                {
                    new FunctionConfiguration {FunctionName = "Function-3"}
                }
            };

            var lambdaMock = new Mock<IAmazonLambda>();
            lambdaMock.Setup(s => s.ListFunctionsAsync(
                It.Is<ListFunctionsRequest>(r => r.Marker == null),
                It.IsAny<CancellationToken>()
                )).ReturnsAsync(_firstPage);

            lambdaMock.Setup(s => s.ListFunctionsAsync(
                It.Is<ListFunctionsRequest>(r => r.Marker == "token-1"),
                It.IsAny<CancellationToken>()
                )).ReturnsAsync(_secondPage);

            lambdaMock.Setup(s => s.ListFunctionsAsync(
                It.Is<ListFunctionsRequest>(r => r.Marker == "token-2"),
                It.IsAny<CancellationToken>()
                )).ReturnsAsync(_thirdPage);

            _lambdaSource = new LambdaSource(lambdaMock.Object);
        }

        [Test]
        public async Task GetResourcesAsync_MultiplePages_AllFetchedAndReturned()
        {
            // arrange

            // act
            var result = await _lambdaSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result.First(), Is.EqualTo(_firstPage.Functions.Single().FunctionName));
            Assert.That(result.Skip(1).First(), Is.EqualTo(_secondPage.Functions.Single().FunctionName));
            Assert.That(result.Skip(2).First(), Is.EqualTo(_thirdPage.Functions.Single().FunctionName));
        }

        [Test]
        public async Task GetResourcesAsync_SinglePage_FetchedAndReturned()
        {
            // arrange
            _firstPage.NextMarker = null;

            // act
            var result = await _lambdaSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));

            Assert.That(result.First(), Is.EqualTo(_firstPage.Functions.Single().FunctionName));
        }

        [Test]
        public async Task GetResourcesAsync_EmptyResult_EmptyListReturned()
        {
            // arrange
            _firstPage.NextMarker = null;
            _firstPage.Functions = new List<FunctionConfiguration>();

            // act
            var result = await _lambdaSource.GetResourceNamesAsync();

            // assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public async Task GetResouceAsync_ReturnsCorrectResource()
        {
            // arrange
            var secondFunctionName = _secondPage.Functions.First().FunctionName;

            // act
            var result = await _lambdaSource.GetResourceAsync(secondFunctionName);

            // assert
            Assert.That(result, Is.InstanceOf<FunctionConfiguration>());
            Assert.That(result.FunctionName, Is.EqualTo(secondFunctionName));
        }
    }
}
