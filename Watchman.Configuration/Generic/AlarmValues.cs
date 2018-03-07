namespace Watchman.Configuration.Generic
{
    public class AlarmValues
    {
        public double? Threshold { get; }
        public int? EvaluationPeriods { get; }
        public string ExtendedStatistic { get; }
        public bool? Enabled { get; }

        public AlarmValues()
        {
            Threshold = null;
            EvaluationPeriods = null;
            Enabled = null;
        }

        public AlarmValues(double? value, int? evaluationPeriods, string extendedStatistic, bool? enabled = null)
        {
            Threshold = value;
            EvaluationPeriods = evaluationPeriods;
            ExtendedStatistic = extendedStatistic;
            Enabled = enabled;
        }

        public static implicit operator AlarmValues(double? threshold)
        {
            return new AlarmValues(threshold, null, null);
        }
    }
}
