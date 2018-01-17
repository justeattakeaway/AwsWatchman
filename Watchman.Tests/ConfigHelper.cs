using System.Collections.Generic;
using Moq;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Configuration.Load;

namespace Watchman.Tests
{
    public static class ConfigHelper
    {
        public static WatchmanConfiguration CreateBasicConfiguration(
            string name,
            string suffix,
            string serviceName,
            List<ResourceThresholds> resources
        )
        {
            return CreateBasicConfiguration(name,
                suffix,
                new Dictionary<string, AwsServiceAlarms>()
                {
                    {
                        serviceName, new AwsServiceAlarms()
                        {
                            Resources = resources
                        }
                    }
                });
        }

        public static WatchmanConfiguration CreateBasicConfiguration(
            string name,
            string suffix,
            Dictionary<string, AwsServiceAlarms> services
        )
        {
            return new WatchmanConfiguration()
            {
                AlertingGroups = new List<AlertingGroup>()
                {
                    new AlertingGroup()
                    {
                        Name = name,
                        AlarmNameSuffix = suffix,
                        Targets = new List<AlertTarget>()
                        {
                            new AlertEmail("test@example.com")
                        },
                        Services = services
                    }
                }
            };
        }

        public static IConfigLoader ConfigLoaderFor(WatchmanConfiguration config)
        {
            var fake = new Mock<IConfigLoader>();
            fake
                .Setup(x => x.LoadConfig())
                .Returns(config);

            return fake.Object;
        }
    }
}
