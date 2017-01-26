namespace Watchman.Configuration.Generic
{
    public class ThresholdValue
    {
        public double Threshold { get; }
        public int? EvaluationPeriods { get; }

        public ThresholdValue()
        {
            Threshold = 0;
            EvaluationPeriods = null;
        }

        public ThresholdValue(double value, int? evaluationPeriods)
        {
            Threshold = value;
            EvaluationPeriods = evaluationPeriods;
        }

        public static implicit operator ThresholdValue(double threshold)
        {
            return new ThresholdValue(threshold, null);
        }
    }
}
