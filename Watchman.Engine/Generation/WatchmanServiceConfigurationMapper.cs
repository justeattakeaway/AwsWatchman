using System;
using System.Collections.Generic;
using System.Linq;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine.Alarms;

namespace Watchman.Engine.Generation
{
    public static class WatchmanServiceConfigurationMapper
    {
        public static WatchmanServiceConfiguration MapRds(WatchmanConfiguration input)
        {
            const string id = "Rds";
            return Map(input, id, a => a?.Services?.Rds, Defaults.Rds);
        }

        public static WatchmanServiceConfiguration MapAutoScaling(WatchmanConfiguration input)
        {
            const string id = "AutoScaling";
            return Map(input, id, a => a?.Services?.AutoScaling, Defaults.AutoScaling);
        }

        public static WatchmanServiceConfiguration MapLambda(WatchmanConfiguration input)
        {
            const string id = "Lambda";
            return Map(input,id, a => a?.Services?.Lambda, Defaults.Lambda);
        }

        public static WatchmanServiceConfiguration MapVpcSubnet(WatchmanConfiguration input)
        {
            const string id = "VpcSubnet";
            return Map(input, id, a => a?.Services?.VpcSubnet, Defaults.VpcSubnets);
        }

        public static WatchmanServiceConfiguration MapElb(WatchmanConfiguration input)
        {
            const string id = "Elb";
            return Map(input, id, a => a?.Services?.Elb, Defaults.Elb);
        }

        public static WatchmanServiceConfiguration MapStream(WatchmanConfiguration input)
        {
            const string id = "KinesisStream";
            return Map(input, id, a => a?.Services?.KinesisStream, Defaults.KinesisStream);
        }

        public static WatchmanServiceConfiguration MapStepFunction(WatchmanConfiguration input)
        {
            const string id = "StepFunction";
            return Map(input, id, a => a?.Services?.StepFunction, Defaults.StepFunction);
        }

        public static WatchmanServiceConfiguration MapDynamoDb(WatchmanConfiguration input)
        {
            const string id = "DynamoDb";
            return Map(input, id, a => a?.Services?.DynamoDb, Defaults.DynamoDb);
        }

        private static WatchmanServiceConfiguration Map(WatchmanConfiguration input,
            string serviceName,
            Func<AlertingGroup, AwsServiceAlarms> readServiceFromGroup,
            IList<AlarmDefinition> defaults)
        {
            var groups = input.AlertingGroups
                .Select(x => ServiceAlertingGroup(x, readServiceFromGroup))
                .Where(x => x != null)
                .ToList();

            return new WatchmanServiceConfiguration(serviceName, groups, defaults);
        }

        private static ServiceAlertingGroup Map(AlertingGroup input, AwsServiceAlarms service)
        {
            return new ServiceAlertingGroup
            {
                GroupParameters = new AlertingGroupParameters(
                    input.Name,
                    input.AlarmNameSuffix,
                    input.Targets,
                    input.IsCatchAll
                ),
                Service = service
            };
        }

        private static ServiceAlertingGroup ServiceAlertingGroup(AlertingGroup ag, Func<AlertingGroup, AwsServiceAlarms> readServiceFromGroup)
        {
            var service = readServiceFromGroup(ag);
            if (service == null)
            {
                return null;
            }

            return Map(ag, service);
        }
    }
}
