using Watchman.Configuration.Generic;

namespace Watchman.Engine.Generation
{
    static class ServiceAlarmConfigExtensions
    {
        public static TAlarmConfig OverrideWith<TAlarmConfig>(this TAlarmConfig serviceConfig,
            TAlarmConfig resourceConfig) where TAlarmConfig : class, IServiceAlarmConfig<TAlarmConfig>, new()
        {
            if (resourceConfig == null)
            {
                return serviceConfig ?? new TAlarmConfig();
            }

            if (serviceConfig == null)
            {
                return resourceConfig;
            }

            return resourceConfig.Merge(serviceConfig);
        }
    }
}