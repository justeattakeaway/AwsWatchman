using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Moq;
using NUnit.Framework;
using Watchman.AwsResources.Services.DynamoDb;

namespace Watchman.AwsResources.Tests.Services.DynamoDb
{
    [TestFixture]
    public class TableDescriptionSourceTests
    {
        private ListTablesResponse _firstPage;
        private ListTablesResponse _secondPage;
        private ListTablesResponse _thirdPage;

        private TableDescriptionSource _tableDescriptionSource;

        [SetUp]
        public void Setup()
        {
            var firstTableName = "Table-1";
            _firstPage = new ListTablesResponse
            {
                LastEvaluatedTableName = firstTableName,
                TableNames = new List<string>
                {
                    firstTableName
                }
            };
            var secondTableName = "Table-2";
            _secondPage = new ListTablesResponse
            {
                LastEvaluatedTableName = secondTableName,
                TableNames = new List<string>
                {
                    secondTableName
                }
            };
            _thirdPage = new ListTablesResponse
            {
                TableNames = new List<string>
                {
                    "Table-3"
                }
            };

            var describeSecondTableResponse = new DescribeTableResponse
            {
                Table = new TableDescription
                {
                    TableName = secondTableName
                }
            };

            var dynamoDbMock = new Mock<IAmazonDynamoDB>();
            dynamoDbMock.Setup(s => s.ListTablesAsync(
                It.Is<string>(r => r == null), It.IsAny<CancellationToken>()
                )).ReturnsAsync(_firstPage);

            dynamoDbMock.Setup(s => s.ListTablesAsync(
                It.Is<string>(r => r == firstTableName),
                It.IsAny<CancellationToken>()
                )).ReturnsAsync(_secondPage);

            dynamoDbMock.Setup(s => s.ListTablesAsync(
                It.Is<string>(r => r == secondTableName),
                It.IsAny<CancellationToken>()
                )).ReturnsAsync(_thirdPage);

            dynamoDbMock.Setup(s => s.DescribeTableAsync(It.Is<string>(r => r == secondTableName),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(describeSecondTableResponse);

            _tableDescriptionSource = new TableDescriptionSource(dynamoDbMock.Object);
        }

        [Test]
        public async Task GetResourcesAsync_MultiplePages_AllFetchedAndReturned()
        {
            // arrange

            // act
            var result = await _tableDescriptionSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(3));

            Assert.That(result.First(), Is.EqualTo(_firstPage.TableNames.Single()));
            Assert.That(result.Skip(1).First(), Is.EqualTo(_secondPage.TableNames.Single()));
            Assert.That(result.Skip(2).First(), Is.EqualTo(_thirdPage.TableNames.Single()));
        }

        [Test]
        public async Task GetResourcesAsync_SinglePage_FetchedAndReturned()
        {
            // arrange
            _firstPage.LastEvaluatedTableName = null;

            // act
            var result = await _tableDescriptionSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));

            Assert.That(result.First(), Is.EqualTo(_firstPage.TableNames.Single()));
        }

        [Test]
        public async Task GetResourcesAsync_EmptyResult_EmptyListReturned()
        {
            // arrange
            _firstPage.LastEvaluatedTableName = null;
            _firstPage.TableNames = new List<string>();

            // act
            var result = await _tableDescriptionSource.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetResouceAsync_ReturnsCorrectResource()
        {
            // arrange
            var secondDbInstanceName = _secondPage.TableNames.First();

            // act
            var result = await _tableDescriptionSource.GetResourceAsync(secondDbInstanceName);

            // assert
            Assert.That(result.Name, Is.EqualTo(secondDbInstanceName));
            Assert.That(result.Resource, Is.InstanceOf<TableDescription>());
            Assert.That(result.Resource.TableName, Is.EqualTo(secondDbInstanceName));
        }
    }
}
