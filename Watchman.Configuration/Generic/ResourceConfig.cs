namespace Watchman.Configuration.Generic
{
    public class ResourceConfig : IServiceAlarmConfig<ResourceConfig>
    {
        public ResourceConfig Merge(ResourceConfig parentConfig)
        {
            if (parentConfig == null)
            {
                throw new ArgumentNullException(nameof(parentConfig));
            }

            return this;
        }
    }
}
