namespace Watchman.Configuration.Generic
{
    public class AlarmValues
    {
        public double? Threshold { get; }
        public int? EvaluationPeriods { get; }
        public string ExtendedStatistic { get; }

        public AlarmValues()
        {
            Threshold = null;
            EvaluationPeriods = null;
        }

        public AlarmValues(double? value, int? evaluationPeriods, string extendedStatistic)
        {
            Threshold = value;
            EvaluationPeriods = evaluationPeriods;
            ExtendedStatistic = extendedStatistic;
        }

        public static implicit operator AlarmValues(double? threshold)
        {
            return new AlarmValues(threshold, null, null);
        }
    }
}
