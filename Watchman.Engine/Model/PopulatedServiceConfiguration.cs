using Watchman.AwsResources;
using Watchman.Configuration.Generic;

namespace Watchman.Engine
{
    public class PopulatedServiceAlertingGroup<TServiceConfig, TResource>
        where TServiceConfig : class where TResource : class
    {
        public AlertingGroupParameters GroupParameters { get; set; }

        public PopulatedServiceAlarms<TServiceConfig, TResource> Service { get; set; }
    }

    public class PopulatedServiceConfiguration<TConfigType, TResourceType>
        where TConfigType : class
        where TResourceType: class
    {
        public string ServiceName { get; }
        public IList<PopulatedServiceAlertingGroup<TConfigType,TResourceType>> AlertingGroups { get; }

        public PopulatedServiceConfiguration(string serviceName,
            IList<PopulatedServiceAlertingGroup<TConfigType, TResourceType>> alertingGroups)
        {
            ServiceName = serviceName;
            AlertingGroups = alertingGroups;
        }
    }

    public class PopulatedServiceAlarms<TResourceConfig, TResource>
        where TResourceConfig : class
        where TResource : class
    {
        public List<ResourceAndThresholdsPair<TResourceConfig, TResource>> Resources { get; set; }

        public List<string> ExcludeResourcesPrefixedWith { get; set; }

        public Dictionary<string, AlarmValues> Values { get; set; }

        public TResourceConfig Options { get; set; }
    }

    public sealed class ResourceAndThresholdsPair<TConfig, TResource>
        where TConfig : class
        where TResource : class
    {

        public ResourceAndThresholdsPair(ResourceThresholds<TConfig> config, AwsResource<TResource> resource)
        {
            Definition = config;
            Resource = resource;
        }

        public AwsResource<TResource> Resource { get; set; }

        public ResourceThresholds<TConfig> Definition { get; set; }
    }
}
