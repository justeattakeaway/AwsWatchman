using NUnit.Framework;

namespace Watchman.Configuration.Tests.Validation
{
    public class DynamoDbConfigValidatorTests
    {
        private DynamoDb _dynamoDb;
        private WatchmanConfiguration _config;

        [SetUp]
        public void Setup()
        {
            _dynamoDb = new DynamoDb
            {
                Threshold = 0.7,
                ThrottlingThreshold = 42,
                Tables = new List<Table>
                {
                    new Table
                    {
                        Name = "TableName",
                        Threshold = 0.8
                    }
                }
            };

            _config = ConfigTestData.ValidConfig();
            _config.AlertingGroups.First().DynamoDb = _dynamoDb;
        }

        [Test]
        public void DynamoDbConfig_Fails_When_Threshold_Is_Too_Low()
        {
            // arrange
            _dynamoDb.Threshold = 0.0;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '0' must be greater than zero");
        }

        [Test]
        public void DynamoDbConfig_Fails_When_Threshold_Is_Too_High()
        {
            // arrange
            _dynamoDb.Threshold = 1.1;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Threshold of '1.1' must be less than or equal to one");
        }

        [Test]
        public void DynamoDbConfig_Fails_With_Invalid_ThrottlingThreshold()
        {
            // arrange
            _dynamoDb.ThrottlingThreshold = -42;

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "AlertingGroup 'someName' has DynamoDb with an invalid throttling threshold of -42");
        }

        [Test]
        public void DynamoDbConfig_Fails_When_Table_Is_Null()
        {
            // arrange
            _dynamoDb.Tables.Add(null);

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "AlertingGroup 'someName' has a null table");
        }

        [Test]
        public void DynamoDbConfig_Fails_When_Table_Has_No_Name_Or_Pattern()
        {
            // arrange
            _dynamoDb.Tables.First().Name = null;

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "AlertingGroup 'someName' has a table with no name or pattern");
        }

        [Test]
        public void DynamoDbConfig_Fails_When_Table_Has_Name_And_Pattern()
        {
            // arrange
            _dynamoDb.Tables.First().Pattern = "TablePattern";

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "AlertingGroup 'someName' has a table 'TableName' with a name and a pattern");
        }

        [Test]
        public void DynamoDbConfig_Fails_When_Table_Threshold_Is_Too_Low()
        {
            // arrange
            _dynamoDb.Tables.First().Threshold = -4.2;

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "Threshold of '-4.2' must be greater than zero");
        }

        [Test]
        public void DynamoDbConfig_Fails_When_Table_Threshold_Is_Too_High()
        {
            // arrange
            _dynamoDb.Tables.First().Threshold = 100500;

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "Threshold of '100500' must be less than or equal to one");
        }

        [Test]
        public void DynamoDbConfig_Fails_When_Throtting_Threshold_Is_Negative()
        {
            // arrange
            _dynamoDb.Tables.First().ThrottlingThreshold = -1;

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "Throttling threshold of '-1' must be greater than zero");
        }

        [Test]
        public void DynamoDbConfig_Full_Passes()
        {
            // arrange

            // act

            // assert
            ConfigAssert.IsValid(_config);
        }

        [Test]
        public void SqsConfig_OnlyTable_Passes()
        {
            // arrange
            _config.AlertingGroups.First().DynamoDb = new DynamoDb
            {
                Tables = new List<Table>
                {
                    new Table {Pattern = "TablePattern"}
                }
            };

            // act

            // assert
            ConfigAssert.IsValid(_config);
        }
    }
}
