namespace Watchman.Configuration.Tests.Validation
{
    public static class ConfigTestData
    {
        public static WatchmanConfiguration ValidConfig()
        {
            return new WatchmanConfiguration
            {
                AlertingGroups = new List<AlertingGroup>
                {
                    new AlertingGroup
                    {
                        Name = "someName",
                        AlarmNameSuffix = "someSuffix",
                        Targets = new List<AlertTarget>
                        {
                            new AlertEmail("foo@bar.com")
                        }
                    }
                }
            };
        }
    }
}
