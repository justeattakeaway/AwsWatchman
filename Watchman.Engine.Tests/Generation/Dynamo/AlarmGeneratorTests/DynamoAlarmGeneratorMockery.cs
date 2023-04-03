using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Moq;
using Watchman.AwsResources;
using Watchman.Engine.Alarms;
using Watchman.Engine.Generation.Dynamo;
using Watchman.Engine.Generation.Dynamo.Alarms;
using Watchman.Engine.LegacyTracking;
using Watchman.Engine.Logging;
using Watchman.Engine.Sns;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    public class DynamoAlarmGeneratorMockery
    {
        public DynamoAlarmGeneratorMockery()
        {
            Cloudwatch = new Mock<IAmazonCloudWatch>();
            AlarmFinder = new Mock<IAlarmFinder>();

            SnsTopicCreator = new Mock<ISnsTopicCreator>();
            SnsSubscriptionCreator = new Mock<ISnsSubscriptionCreator>();

            TableLoader = new Mock<IResourceSource<TableDescription>>();

            var logger = new ConsoleAlarmLogger(false);
            var tableNamePopulator = new TableNamePopulator(logger, TableLoader.Object);
            var snsCreator = new SnsCreator(SnsTopicCreator.Object, SnsSubscriptionCreator.Object);

            var tableAlarmCreator = new TableAlarmCreator(Cloudwatch.Object, AlarmFinder.Object, logger, Mock.Of<ILegacyAlarmTracker>());
            var indexAlarmCreator = new IndexAlarmCreator(Cloudwatch.Object, AlarmFinder.Object, logger, Mock.Of<ILegacyAlarmTracker>());

            AlarmGenerator = new DynamoAlarmGenerator(
                logger,
                tableNamePopulator,
                tableAlarmCreator,
                indexAlarmCreator,
                snsCreator,
                TableLoader.Object);
        }

        public Mock<IResourceSource<TableDescription>> TableLoader { get; set; }

        public DynamoAlarmGenerator AlarmGenerator { get; private set; }

        public Mock<ISnsSubscriptionCreator> SnsSubscriptionCreator { get; }

        public Mock<ISnsTopicCreator> SnsTopicCreator { get; }

        public Mock<IAlarmFinder> AlarmFinder { get; set; }

        public Mock<IAmazonCloudWatch> Cloudwatch { get; set; }


        public void GivenAListOfTables(IEnumerable<string> tableNames)
        {
            TableLoader.Setup(x => x.GetResourceNamesAsync())
                .ReturnsAsync(tableNames.ToList());
        }

        public void GivenATable(string tableName, int readCapacity, int writeCapacity)
        {
            var tableDesc = new TableDescription
                {
                    TableName = tableName,
                    ProvisionedThroughput = new ProvisionedThroughputDescription
                    {
                        ReadCapacityUnits = readCapacity,
                        WriteCapacityUnits = writeCapacity
                    }
                };
            TableLoader
                .Setup(x => x.GetResourceAsync(tableName))
                .ReturnsAsync(tableDesc);
        }

        public void GivenATableWithIndex(string tableName, string indexName, int indexRead, int indexWrite)
        {
            var tableDesc = new TableDescription
                {
                    TableName = tableName,
                    ProvisionedThroughput = new ProvisionedThroughputDescription
                    {
                        ReadCapacityUnits = indexRead,
                        WriteCapacityUnits = indexWrite
                    },
                    GlobalSecondaryIndexes = new List<GlobalSecondaryIndexDescription>
                    {
                        new GlobalSecondaryIndexDescription
                        {
                            IndexName = indexName,
                            ProvisionedThroughput = new ProvisionedThroughputDescription
                            {
                                ReadCapacityUnits = indexRead,
                                WriteCapacityUnits = indexWrite
                            }
                        }
                    }
                };

            TableLoader
                .Setup(x => x.GetResourceAsync(tableName))
                .ReturnsAsync(tableDesc);
        }

        public void GivenATableDoesNotExist(string failureTable)
        {
            TableLoader
                .Setup(x => x.GetResourceAsync(failureTable))
                .Throws(new AmazonDynamoDBException("The table does not exist"));
        }


        public void ValidSnsTopic()
        {
            SnsTopicCreator.Setup(x => x.EnsureSnsTopic(It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync("sns-topic-arn");
        }

        public void GivenCreatingATopicWillReturnAnArn(string alertingGroupName, string snsTopicArn)
        {
            SnsTopicCreator.Setup(x => x.EnsureSnsTopic(alertingGroupName, It.IsAny<bool>()))
                .ReturnsAsync(snsTopicArn);
        }
    }
}
