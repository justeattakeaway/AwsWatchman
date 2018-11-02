using System.Threading.Tasks;
using Watchman.AwsResources;
using Watchman.Configuration;
using Watchman.Configuration.Generic;

namespace Watchman.Engine.Generation
{
    static class ThresholdCalculator
    {
        public static async Task<Threshold> ExpandThreshold<T, TAlarmConfig>(
            IResourceAttributesProvider<T, TAlarmConfig> attributeProvider,
            T resource,
            TAlarmConfig config,
            Threshold threshold)
            where TAlarmConfig : class, IServiceAlarmConfig<TAlarmConfig>, new()
        {
            if (threshold.ThresholdType == ThresholdType.PercentageOf)
            {
                var fraction = threshold.Value / 100;
                var property = await attributeProvider.GetValue(resource, config, threshold.SourceAttribute);

                threshold = new Threshold
                {
                    ThresholdType = ThresholdType.Absolute,
                    Value = fraction * (double) property
                };
            }

            return threshold;
        }
    }
}
