namespace Watchman.Configuration.Generic
{
    public class AlarmValues
    {
        public double? Threshold { get; }
        public int? EvaluationPeriods { get; }
        public string Statistic { get; }
        public string ExtendedStatistic { get; }
        public bool? Enabled { get; }
        public int? PeriodMinutes { get; }

        public AlarmValues()
        {
            Threshold = null;
            EvaluationPeriods = null;
            Enabled = null;
            PeriodMinutes = null;
        }

        public AlarmValues(double? value = null,
            int? evaluationPeriods = null,
            string statistic = null,
            string extendedStatistic = null,
            bool? enabled = null,
            int? periodMinutes = null)
        {
            Threshold = value;
            EvaluationPeriods = evaluationPeriods;
            Statistic = statistic;
            ExtendedStatistic = extendedStatistic;
            Enabled = enabled;
            PeriodMinutes = periodMinutes;
        }

        public static implicit operator AlarmValues(double? threshold)
        {
            return new AlarmValues(threshold, null, null);
        }
    }
}
