﻿using NUnit.Framework;
using Watchman.Configuration.Generic;

namespace Watchman.Configuration.Tests.Validation
{
    [TestFixture]
    public class ConfigValidatorTests
    {
        private WatchmanConfiguration _config;

        [SetUp]
        public void Setup()
        {
            _config = ConfigTestData.ValidConfig();

            _config.AlertingGroups.First().DynamoDb.Tables = new List<Table>
                {
                    new Table { Name = "TableName" }
                };
            _config.AlertingGroups.First().Sqs = new Sqs
            {
                Queues = new List<Queue>
                {
                    new Queue {Name = "QueueName"}
                }
            };
            _config.AlertingGroups.First().Services = new AlertingGroupServices()
            {
                AutoScaling = new AwsServiceAlarms<AutoScalingResourceConfig>
                {
                    Resources = new List<ResourceThresholds<AutoScalingResourceConfig>>
                    {
                        new ResourceThresholds<AutoScalingResourceConfig> {Pattern = "ResourceName"}
                    }
                }
            };

        }

        [Test]
        public void Config_Fails_When_Null()
        {
            // arrange
            _config = null;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Config cannot be null");
        }

        [Test]
        public void Config_Fails_When_AlertingGroups_Is_Null()
        {
            // arrange
            _config.AlertingGroups = null;

            // act

            // assert
            ConfigAssert.NotValid(_config, "Config must have alerting groups");
        }

        [Test]
        public void Config_Fails_When_AlertingGroups_Is_Empty()
        {
            // arrange
            _config.AlertingGroups = new List<AlertingGroup>();

            // act

            // assert
            ConfigAssert.NotValid(_config, "Config must have alerting groups");
        }

        [Test]
        public void AlertingGroup_Fails_Without_Name()
        {
            // arrange
            _config.AlertingGroups.First().Name = null;

            // act

            // assert
            ConfigAssert.NotValid(_config, "AlertingGroup must have a name");
        }

        [Test]
        public void AlertingGroup_Fails_When_Name_Is_Too_Long()
        {
            // arrange
            var tooLongName = "here_should_be_more_then_100_symbols_here_should_be_more_then_100_symbols_here_should_be_more_then_100_symbols";
            _config.AlertingGroups.First().Name = tooLongName;

            // act

            // assert
            ConfigAssert.NotValid(_config, $"AlertingGroup name '{tooLongName}' must be valid in SNS topics");
        }

        [Test]
        public void AlertingGroup_Fails_When_Name_Contains_Incorrect_Symbols()
        {
            // arrange
            var incorrectName = "this_symbols_are_incorrect_for_name_%^&*@";
            _config.AlertingGroups.First().Name = incorrectName;

            // act

            // assert
            ConfigAssert.NotValid(_config, $"AlertingGroup name '{incorrectName}' must be valid in SNS topics");
        }

        [Test]
        public void AlertingGroup_Fails_Without_AlarmNameSuffix()
        {
            // arrange
            _config.AlertingGroups.First().AlarmNameSuffix = null;

            // act

            // assert
            ConfigAssert.NotValid(_config, "AlertingGroup 'someName' must have an alarm suffix");
        }

        [Test]
        public void AlertingGroup_Fails_When_AlarmNameSuffix_Is_Too_Long()
        {
            // arrange
            var tooLongName = "here_should_be_more_then_100_symbols_here_should_be_more_then_100_symbols_here_should_be_more_then_100_symbols";
            _config.AlertingGroups.First().AlarmNameSuffix = tooLongName;

            // act

            // assert
            ConfigAssert.NotValid(_config,
                $"AlertingGroup 'someName' must have a suffix valid in SNS topics. '{tooLongName}' is not.");
        }

        [Test]
        public void AlertingGroup_Fails_When_AlarmNameSuffix_Contains_Incorrect_Symbols()
        {
            // arrange
            var incorrectName = "this_symbols_are_incorrect_for_name_%^&*@";
            _config.AlertingGroups.First().AlarmNameSuffix = incorrectName;

            // act

            // assert
            ConfigAssert.NotValid(_config,
                $"AlertingGroup 'someName' must have a suffix valid in SNS topics. '{incorrectName}' is not.");
        }

        [Test]
        public void AlertingGroup_Fails_When_Targets_Is_Null()
        {
            // arrange
            _config.AlertingGroups.First().Targets = null;

            // act

            // assert
            ConfigAssert.NotValid(_config, "AlertingGroup 'someName' must have targets");
        }

        [Test]
        public void AlertingGroup_Succeeds_When_Targets_Is_Empty()
        {
            // arrange
            _config.AlertingGroups.First().Targets = new List<AlertTarget>();

            // act

            // assert
            ConfigAssert.IsValid(_config);
        }

        [Test]
        public void AlertingGroup_Succeeds_With_Valid_Email_Target()
        {
            // arrange
            _config.AlertingGroups.First().Targets = new List<AlertTarget>
            {
                new AlertEmail("homer.j.simpson@springfieldnuclear.com")
            };

            // act

            // assert
            ConfigAssert.IsValid(_config);
        }

        [Test]
        public void AlertingGroup_Fails_With_Empty_Email_Target()
        {
            // arrange
            _config.AlertingGroups.First().Targets = new List<AlertTarget>
            {
                new AlertEmail(string.Empty)
            };

            // act

            // assert
            ConfigAssert.NotValid(_config, "Email target for AlertingGroup 'someName' must have an email address");
        }

        [Test]
        public void AlertingGroup_Succeeds_With_Valid_Url_Target()
        {
            // arrange
            _config.AlertingGroups.First().Targets = new List<AlertTarget>
            {
                new AlertUrl("http://api.example.com/foo/bar")
            };

            // act

            // assert
            ConfigAssert.IsValid(_config);
        }

        [Test]
        public void AlertingGroup_Fails_With_Empty_Url_Target()
        {
            // arrange
            _config.AlertingGroups.First().Targets = new List<AlertTarget>
            {
                new AlertUrl(string.Empty)
            };

            // act

            // assert
            ConfigAssert.NotValid(_config, "Url target for AlertingGroup 'someName' must have a url");
        }

        [Test]
        public void AlertingGroup_Fails_With_Invalid_Url_Target()
        {
            // arrange
            _config.AlertingGroups.First().Targets = new List<AlertTarget>
            {
                new AlertUrl("fish")
            };

            // act

            // assert
            ConfigAssert.NotValid(_config, "Url target 'fish' for AlertingGroup 'someName' is not valid");
        }
        
        [Test]
        public void AlertingGroup_All_Resources_Succeeds()
        {
            // arrange

            // act

            // assert
            ConfigAssert.IsValid(_config);
        }
    }
}
