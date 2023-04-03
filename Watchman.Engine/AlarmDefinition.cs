using Amazon.CloudWatch;
using Watchman.Configuration;
using Watchman.Configuration.Generic;

namespace Watchman.Engine
{
    /// <summary>
    /// Alarm which will be applied
    /// </summary>
    public class AlarmDefinition
    {
        public string Name { get; set; }
        public string Metric { get; set; }
        public Statistic Statistic { get; set; }
        public string ExtendedStatistic { get; set; }
        public TimeSpan Period { get; set; }
        public int EvaluationPeriods { get; set; }
        public Threshold Threshold { get; set; }
        public ComparisonOperator ComparisonOperator { get; set; }
        public string Namespace { get; set; }
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// A list of Dimension names. The values are obtained from the specific resource.
        /// </summary>
        public IList<string> DimensionNames { get; set; }

        /// <summary>
        /// Should send an alert on a transition into the "InsufficientData" state ?
        /// </summary>
        public bool AlertOnInsufficientData { get; set; }

        /// <summary>
        /// Should send an alert on a transition into the "Ok" state ?
        /// This is useful to get the alert to automatically reset when leaving the error state
        /// </summary>
        public bool AlertOnOk { get; set; } = true;

        // <summary>
        // Sets how this alarm is to handle missing data points.
        // </summary>
        public string TreatMissingData { get; set; } = TreatMissingDataConstants.Missing;

        public AlarmDefinition Copy()
        {
            return new AlarmDefinition
            {
                EvaluationPeriods = EvaluationPeriods,
                Metric = Metric,
                Name = Name,
                Period = Period,
                Statistic = Statistic,
                Threshold = Threshold,
                ComparisonOperator = ComparisonOperator,
                DimensionNames = DimensionNames,
                Namespace = Namespace,
                AlertOnInsufficientData = AlertOnInsufficientData,
                AlertOnOk = AlertOnOk,
                ExtendedStatistic = ExtendedStatistic,
                TreatMissingData = TreatMissingData
            };
        }

        public AlarmDefinition CopyWith(
            Threshold threshold,
            AlarmValues mergedValues)
        {
            var copy = Copy();

            copy.Threshold = threshold;
            copy.EvaluationPeriods = mergedValues.EvaluationPeriods ?? EvaluationPeriods;

            copy.Statistic = IsValidStatistic(mergedValues.Statistic)
                ? new Statistic(mergedValues.Statistic)
                : Statistic;

            copy.ExtendedStatistic = !string.IsNullOrEmpty(mergedValues.ExtendedStatistic)
                ? mergedValues.ExtendedStatistic
                : ExtendedStatistic;

            copy.Period = mergedValues.PeriodMinutes != null
                          ? TimeSpan.FromMinutes(mergedValues.PeriodMinutes.Value)
                          : Period;

            copy.Enabled = mergedValues.Enabled ?? Enabled;

            return copy;
        }

        private bool IsValidStatistic(string statistic)
        {
            if (string.IsNullOrEmpty(statistic))
                return false;

            switch (statistic)
            {
                case nameof(Statistic.Average):
                case nameof(Statistic.Maximum):
                case nameof(Statistic.Minimum):
                case nameof(Statistic.SampleCount):
                case nameof(Statistic.Sum):
                    return true;
                default:
                    return false;
            }
        }
    }
}
