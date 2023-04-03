using Watchman.Configuration;

namespace Watchman.Engine.Tests.Generation.Dynamo.AlarmGeneratorTests
{
    public static class AlertingGroupData
    {
        public static WatchmanConfiguration WrapGroup(AlertingGroup group)
        {
            return new WatchmanConfiguration
            {
                AlertingGroups = new List<AlertingGroup> { group }
            };
        }

        public static WatchmanConfiguration WrapDynamo(DynamoDb dynamo)
        {
            var ag = new AlertingGroup
            {
                Name = "TestGroup",
                AlarmNameSuffix = "TestGroup",
                IsCatchAll = true,
                Targets = new List<AlertTarget>
                {
                    new AlertEmail("test.user@test-org.com")
                },
                DynamoDb = dynamo
            };

            return WrapGroup(ag);
        }
    }
}
