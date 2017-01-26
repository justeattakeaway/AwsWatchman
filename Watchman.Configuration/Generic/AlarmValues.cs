namespace Watchman.Configuration.Generic
{
    public class AlarmValues
    {
        public double? Threshold { get; }
        public int? EvaluationPeriods { get; }

        public AlarmValues()
        {
            Threshold = null;
            EvaluationPeriods = null;
        }

        public AlarmValues(double? value, int? evaluationPeriods)
        {
            Threshold = value;
            EvaluationPeriods = evaluationPeriods;
        }

        public static implicit operator AlarmValues(double? threshold)
        {
            return new AlarmValues(threshold, null);
        }
    }
}
