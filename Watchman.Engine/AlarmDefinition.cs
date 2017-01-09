using System;
using System.Collections.Generic;
using Amazon.CloudWatch;
using Watchman.Configuration;

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
        public TimeSpan Period { get; set; }
        public int EvaluationPeriods { get; set; }
        public Threshold Threshold { get; set; }
        public ComparisonOperator ComparisonOperator { get; set; }
        public string Namespace { get; set; }

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
        public bool AlertOnOk { get; set; }

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
                AlertOnOk = AlertOnOk
            };
        }
    }
}
