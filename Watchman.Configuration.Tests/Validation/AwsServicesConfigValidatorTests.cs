﻿using NUnit.Framework;
using Watchman.Configuration.Generic;

namespace Watchman.Configuration.Tests.Validation
{
    [TestFixture]
    public class AwsServicesConfigValidatorTests
    {
        private AwsServiceAlarms<ResourceConfig> _awsServiceAlarms;
        private WatchmanConfiguration _config;

        [SetUp]
        public void Setup()
        {
            _awsServiceAlarms = new AwsServiceAlarms<ResourceConfig>
            {
                ExcludeResourcesPrefixedWith = new List<string>
                {
                    "ExcludePrefix"
                },
                Resources = new List<ResourceThresholds<ResourceConfig>>
                {
                    new ResourceThresholds<ResourceConfig>
                    {
                        Name = "ResourceName",
                        Values = new Dictionary<string, AlarmValues>
                        {
                            {"testThresholdLow", 42}
                        }
                    }
                },
                Values = new Dictionary<string, AlarmValues>
                {
                    {"testThresholdHigh", 242}
                }
            };

            _config = ConfigTestData.ValidConfig();
            _config.AlertingGroups.First().Services = new AlertingGroupServices()
            {
                Lambda = _awsServiceAlarms
            };
        }

        [Test]
        public void AwsServicesConfig_Fails_When_ServiceThreshold_Is_Negative()
        {
            // arrange
            _awsServiceAlarms.Values.Add("invalidThreshold", -42);

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "Threshold of 'invalidThreshold' must be greater than zero");
        }

        [Test]
        public void AwsServicesConfig_Fails_When_Resource_Is_Null()
        {
            // arrange
            _awsServiceAlarms.Resources.Add(null);

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "AlertingGroup 'someName' has a 'Lambda' Service with null resource");
        }

        [Test]
        public void AwsServicesConfig_Fails_When_There_Is_No_Name_Or_Pattern()
        {
            // arrange
            _awsServiceAlarms.Resources.First().Name = null;

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "AlertingGroup 'someName' has a 'Lambda' Service with no name or pattern");
        }

        [Test]
        public void AwsServicesConfig_Fails_When_ResourceThreshold_Is_Negative()
        {
            // arrange
            _awsServiceAlarms.Resources.First().Values.Add("invalidThreshold", -42);

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "Threshold of 'invalidThreshold' must be greater than zero");
        }

        [Test]
        public void AwsServicesConfig_Full_Passes()
        {
            // arrange

            // act

            // assert
            ConfigAssert.IsValid(_config);
        }

        [Test]
        public void AwsServicesConfig_OnlyResource_Passes()
        {
            // arrange
            _config.AlertingGroups.First().Services = new AlertingGroupServices()
            {
                Lambda = new AwsServiceAlarms<ResourceConfig>
                {
                    Resources = new List<ResourceThresholds<ResourceConfig>>
                    {
                        new ResourceThresholds<ResourceConfig> {Pattern = "ResourcePattern"}
                    }
                }
            };

            // act

            // assert
            ConfigAssert.IsValid(_config);
        }

        [Test]
        public void FailsIfServiceHasNoResources()
        {
            // arrange
            _awsServiceAlarms.Resources = null;

            // act

            // assert
            ConfigAssert.NotValid(_config,
                "AlertingGroup 'someName' has a 'Lambda' Service with missing Resources section");
        }
    }
}
