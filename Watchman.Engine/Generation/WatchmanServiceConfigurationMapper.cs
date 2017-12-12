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
        private static AwsServiceAlarms GetService(AlertingGroup group, string serviceIdentifier)
        {
            if (!group.Services.ContainsKey(serviceIdentifier))
            {
                return null;
            }

            return group.Services[serviceIdentifier];
        }

        public static WatchmanServiceConfiguration MapRds(WatchmanConfiguration input)
        {
            const string id = "Rds";
            return Map(input, id, a => GetService(a, id), Defaults.Rds);
        }

        public static WatchmanServiceConfiguration MapAutoScaling(WatchmanConfiguration input)
        {
            const string id = "AutoScaling";
            return Map(input, id, a => GetService(a, id), Defaults.AutoScaling);
        }

        public static WatchmanServiceConfiguration MapLambda(WatchmanConfiguration input)
        {
            const string id = "Lambda";
            return Map(input,id, a => GetService(a, id), Defaults.Lambda);
        }

        public static WatchmanServiceConfiguration MapVpcSubnet(WatchmanConfiguration input)
        {
            const string id = "VpcSubnet";
            return Map(input, id, a => GetService(a, id), Defaults.VpcSubnets);
        }

        public static WatchmanServiceConfiguration MapElb(WatchmanConfiguration input)
        {
            const string id = "Elb";
            return Map(input, id, a => GetService(a, id), Defaults.Elb);
        }

        public static WatchmanServiceConfiguration MapStream(WatchmanConfiguration input)
        {
            const string id = "KinesisStream";
            return Map(input, id, a => GetService(a, id), Defaults.KinesisStream);
        }

        public static WatchmanServiceConfiguration MapStepFunction(WatchmanConfiguration input)
        {
            const string id = "StepFunction";
            return Map(input, id, a => GetService(a, id), Defaults.StepFunction);
        }

        public static WatchmanServiceConfiguration MapDynamoDb(WatchmanConfiguration input)
        {
            const string id = "DynamoDb";
            return Map(input, id, a => GetService(a, id), Defaults.DynamoDb);
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
                AlarmNameSuffix = input.AlarmNameSuffix,
                IsCatchAll = input.IsCatchAll,
                Name = input.Name,
                ReportTargets = input.ReportTargets,
                Service = service,
                Targets = input.Targets
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
