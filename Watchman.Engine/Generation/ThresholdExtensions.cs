using Watchman.Configuration.Generic;

namespace Watchman.Engine.Generation
{
    static class ThresholdExtensions
    {
        public static Dictionary<string, AlarmValues> OverrideWith(
            this Dictionary<string, AlarmValues> serviceThresholds,
            Dictionary<string, AlarmValues> resourceThresholds)
        {
            var thresholds = new[]
                {
                    resourceThresholds,
                    serviceThresholds
                }
                .Where(t => t != null)
                .ToArray();

            var allKeys = thresholds
                .SelectMany(x => x.Keys)
                .Distinct()
                .ToArray();

            var merged = allKeys
                .Select(key =>
                    {
                        var matchesForKey = thresholds
                            .Where(t => t.ContainsKey(key))
                            .Select(t => t[key])
                            .ToList();

                        var matchedEvalPeriods = matchesForKey
                            .Select(t => t.EvaluationPeriods)
                            .FirstOrDefault(t => t.HasValue);

                        var matchedThresholdValue = matchesForKey
                            .Select(t => t.Threshold)
                            .FirstOrDefault(t => t.HasValue);

                        var matchedStatistic = matchesForKey
                            .Select(t => t.Statistic)
                            .FirstOrDefault(t => !string.IsNullOrEmpty(t));

                        var matchedExtendedStatistic = matchesForKey
                            .Select(t => t.ExtendedStatistic)
                            .FirstOrDefault(t => !string.IsNullOrEmpty(t));

                        var matchedEnabled = matchesForKey
                            .Select(t => t.Enabled)
                            .FirstOrDefault(t => t.HasValue);

                        var matchedPeriodMinutes = matchesForKey
                            .Select(t => t.PeriodMinutes)
                            .FirstOrDefault(t => t.HasValue);

                        var mergedValues = new AlarmValues(matchedThresholdValue,
                            matchedEvalPeriods,
                            matchedStatistic,
                            matchedExtendedStatistic,
                            matchedEnabled,
                            matchedPeriodMinutes);
                        return (key: key, values: mergedValues);
                    }
                ).ToDictionary(x => x.key, x => x.values);

            return merged;
        }
    }
}
