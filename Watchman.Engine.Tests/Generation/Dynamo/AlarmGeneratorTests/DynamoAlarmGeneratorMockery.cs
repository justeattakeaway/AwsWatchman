using Amazon.CloudWatch;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
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
            Cloudwatch = Substitute.For<IAmazonCloudWatch>();
            AlarmFinder = Substitute.For<IAlarmFinder>();

            SnsTopicCreator = Substitute.For<ISnsTopicCreator>();
            SnsSubscriptionCreator = Substitute.For<ISnsSubscriptionCreator>();

            TableLoader = Substitute.For<IResourceSource<TableDescription>>();

            var logger = new ConsoleAlarmLogger(false);
            var tableNamePopulator = new TableNamePopulator(logger, TableLoader);
            var snsCreator = new SnsCreator(SnsTopicCreator, SnsSubscriptionCreator);

            var tableAlarmCreator = new TableAlarmCreator(Cloudwatch, AlarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());
            var indexAlarmCreator = new IndexAlarmCreator(Cloudwatch, AlarmFinder, logger, Substitute.For<ILegacyAlarmTracker>());

            AlarmGenerator = new DynamoAlarmGenerator(
                logger,
                tableNamePopulator,
                tableAlarmCreator,
                indexAlarmCreator,
                snsCreator,
                TableLoader);
        }

        public IResourceSource<TableDescription> TableLoader { get; set; }

        public DynamoAlarmGenerator AlarmGenerator { get; private set; }

        public ISnsSubscriptionCreator SnsSubscriptionCreator { get; }

        public ISnsTopicCreator SnsTopicCreator { get; }

        public IAlarmFinder AlarmFinder { get; set; }

        public IAmazonCloudWatch Cloudwatch { get; set; }


        public void GivenAListOfTables(IEnumerable<string> tableNames)
        {
            TableLoader.GetResourceNamesAsync()
                .Returns(tableNames.ToList());
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
                .GetResourceAsync(tableName)
                .Returns(tableDesc);
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
                .GetResourceAsync(tableName)
                .Returns(tableDesc);
        }

        public void GivenATableDoesNotExist(string failureTable)
        {
            TableLoader
                .GetResourceAsync(failureTable).ThrowsAsync(new AmazonDynamoDBException("The table does not exist"));
        }


        public void ValidSnsTopic()
        {
            SnsTopicCreator.EnsureSnsTopic(Arg.Any<string>(), Arg.Any<bool>())
                .Returns("sns-topic-arn");
        }

        public void GivenCreatingATopicWillReturnAnArn(string alertingGroupName, string snsTopicArn)
        {
            SnsTopicCreator.EnsureSnsTopic(alertingGroupName, Arg.Any<bool>())
                .Returns(snsTopicArn);
        }
    }
}
