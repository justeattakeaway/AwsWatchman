namespace Watchman.Configuration.Generic
{
    public class ThresholdValue
    {
        public double Value { get; set; }
        public int EvaluationPeriods { get; set; } = 1;

        public static implicit operator ThresholdValue(double value)
        {
            return new ThresholdValue
            {
                Value = value,
                EvaluationPeriods = 1
            };
        }
    }
}
