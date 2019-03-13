using System.Collections.Generic;
using System.Linq;
using Moq;
using Watchman.Configuration;
using Watchman.Configuration.Load;

namespace Watchman.Tests
{
    public static class ConfigHelper
    {
        public static WatchmanConfiguration CreateBasicConfiguration(
            string name,
            string suffix,
            AlertingGroupServices services,
            int numberOfCloudFormationStacks = 1
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
                        Services = services,
                        NumberOfCloudFormationStacks = numberOfCloudFormationStacks
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

        public static IConfigLoader ConfigLoaderFor(params AlertingGroup[] groups)
        {
            var fake = new Mock<IConfigLoader>();
            fake
                .Setup(x => x.LoadConfig())
                .Returns(new WatchmanConfiguration() { AlertingGroups = groups.ToList() });

            return fake.Object;
        }
    }
}
