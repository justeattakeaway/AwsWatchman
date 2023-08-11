using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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

        private TableDescriptionSource SetupPagingTest()
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

            var dynamoDbMock = Substitute.For<IAmazonDynamoDB>();
            dynamoDbMock.ListTablesAsync(
                Arg.Is<string>(r => r == null), Arg.Any<CancellationToken>()
            ).Returns(_firstPage);

            dynamoDbMock.ListTablesAsync(
                Arg.Is<string>(r => r == firstTableName),
                Arg.Any<CancellationToken>()
            ).Returns(_secondPage);

            dynamoDbMock.ListTablesAsync(
                Arg.Is<string>(r => r == secondTableName),
                Arg.Any<CancellationToken>()
            ).Returns(_thirdPage);

            dynamoDbMock.DescribeTableAsync(Arg.Is<string>(r => r == secondTableName),
                    Arg.Any<CancellationToken>())
                .Returns(describeSecondTableResponse);

            return new TableDescriptionSource(dynamoDbMock);
        }

        [Test]
        public async Task GetResourcesAsync_MultiplePages_AllFetchedAndReturned()
        {
            // arrange
            var test = SetupPagingTest();

            // act
            var result = await test.GetResourceNamesAsync();

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
            var test = SetupPagingTest();

            _firstPage.LastEvaluatedTableName = null;

            // act
            var result = await test.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(1));

            Assert.That(result.First(), Is.EqualTo(_firstPage.TableNames.Single()));
        }

        [Test]
        public async Task GetResourcesAsync_EmptyResult_EmptyListReturned()
        {
            // arrange
            var test = SetupPagingTest();
            _firstPage.LastEvaluatedTableName = null;
            _firstPage.TableNames = new List<string>();

            // act
            var result = await test.GetResourceNamesAsync();

            // assert
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task GetResouceAsync_ReturnsCorrectResource()
        {
            // arrange
            var test = SetupPagingTest();
            var secondDbInstanceName = _secondPage.TableNames.First();

            // act
            var result = await test.GetResourceAsync(secondDbInstanceName);

            // assert
            Assert.That(result, Is.InstanceOf<TableDescription>());
            Assert.That(result.TableName, Is.EqualTo(secondDbInstanceName));
        }

        [Test]
        public async Task GetResourceAsync_ReturnsNullIfNotInList()
        {
            var result = await SetupPagingTest().GetResourceAsync("does-not-exist");
            Assert.Null(result);
        }

        [Test]
        public async Task GetResourceAsync_ReturnsNullIfSdkThrowsNotFound()
        {
            var dynamoDbFake = Substitute.For<IAmazonDynamoDB>();

            dynamoDbFake
                .ListTablesAsync(
                    Arg.Is<string>(r => r == null), Arg.Any<CancellationToken>()
                )
                .Returns(new ListTablesResponse()
                {
                    TableNames = new List<string>() { "banana" }
                });

            dynamoDbFake
                .DescribeTableAsync("banana", Arg.Any<CancellationToken>()).Throws(new ResourceNotFoundException("Table not found"));

            var sut = new TableDescriptionSource(dynamoDbFake);

            var result = await sut.GetResourceAsync("banana");

            await dynamoDbFake
                .Received()
                .DescribeTableAsync("banana", Arg.Any<CancellationToken>());

            Assert.Null(result);
        }
    }
}
