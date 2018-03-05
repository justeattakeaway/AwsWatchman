using System;
using System.Collections.Generic;
using System.Linq;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Generic;
using Watchman.Engine.Alarms;

namespace Watchman.Engine.Generation
{
    public static class WatchmanServiceConfigurationMapper
    {
        public static WatchmanServiceConfiguration<ResourceConfig> MapRds(WatchmanConfiguration input)
        {
            const string id = "Rds";
            return Map(input, id, a => a?.Services?.Rds, Defaults.Rds);
        }

        public static WatchmanServiceConfiguration<AutoScalingResourceConfig> MapAutoScaling(WatchmanConfiguration input)
        {
            const string id = "AutoScaling";
            return Map(input, id, a => a?.Services?.AutoScaling, Defaults.AutoScaling);
        }

        public static WatchmanServiceConfiguration<ResourceConfig> MapLambda(WatchmanConfiguration input)
        {
            const string id = "Lambda";
            return Map(input, id, a => a?.Services?.Lambda, Defaults.Lambda);
        }

        public static WatchmanServiceConfiguration<ResourceConfig> MapVpcSubnet(WatchmanConfiguration input)
        {
            const string id = "VpcSubnet";
            return Map(input, id, a => a?.Services?.VpcSubnet, Defaults.VpcSubnets);
        }

        public static WatchmanServiceConfiguration<ResourceConfig> MapElb(WatchmanConfiguration input)
        {
            const string id = "Elb";
            return Map(input, id, a => a?.Services?.Elb, Defaults.Elb);
        }

        public static WatchmanServiceConfiguration<ResourceConfig> MapStream(WatchmanConfiguration input)
        {
            const string id = "KinesisStream";
            return Map(input, id, a => a?.Services?.KinesisStream, Defaults.KinesisStream);
        }

        public static WatchmanServiceConfiguration<ResourceConfig> MapStepFunction(WatchmanConfiguration input)
        {
            const string id = "StepFunction";
            return Map(input, id, a => a?.Services?.StepFunction, Defaults.StepFunction);
        }

        public static WatchmanServiceConfiguration<ResourceConfig> MapDynamoDb(WatchmanConfiguration input)
        {
            const string id = "DynamoDb";
            return Map(input, id, a => a?.Services?.DynamoDb, Defaults.DynamoDb);
        }

        private static WatchmanServiceConfiguration<TConfig> Map<TConfig>(WatchmanConfiguration input,
            string serviceName,
            Func<AlertingGroup, AwsServiceAlarms<TConfig>> readServiceFromGroup,
            IList<AlarmDefinition> defaults) where TConfig : class
        {
            var groups = input.AlertingGroups
                .Select(x => ServiceAlertingGroup(x, readServiceFromGroup))
                .Where(x => x != null)
                .ToList();

            return new WatchmanServiceConfiguration<TConfig>(serviceName, groups, defaults);
        }

        private static ServiceAlertingGroup<T> Map<T>(AlertingGroup input, AwsServiceAlarms<T> service) where T:class
        {
            return new ServiceAlertingGroup<T>
            {
                GroupParameters = new AlertingGroupParameters(
                    input.Name,
                    input.AlarmNameSuffix,
                    input.Targets,
                    input.IsCatchAll,
                    input.Description
                ),
                Service = service
            };
        }

        private static ServiceAlertingGroup<T> ServiceAlertingGroup<T>(AlertingGroup ag,
            Func<AlertingGroup, AwsServiceAlarms<T>> readServiceFromGroup) where T: class
        {
            var service = readServiceFromGroup(ag);
            if (service == null)
            {
                return null;
            }

            return Map<T>(ag, service);
        }
    }
}
