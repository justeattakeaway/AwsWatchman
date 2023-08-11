using NSubstitute;
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
            var fake = Substitute.For<IConfigLoader>();
            fake
                .LoadConfig()
                .Returns(config);

            return fake;
        }

        public static IConfigLoader ConfigLoaderFor(params AlertingGroup[] groups)
        {
            var fake = Substitute.For<IConfigLoader>();
            fake
                .LoadConfig()
                .Returns(new WatchmanConfiguration() { AlertingGroups = groups.ToList() });

            return fake;
        }
    }
}
