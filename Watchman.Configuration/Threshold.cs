namespace Watchman.Configuration
{
    public class Threshold
    {
        public ThresholdType ThresholdType { get; set; }
        public double Value { get; set; }
        public int EvaluationPeriods { get; set; } = 1;
        public string SourceAttribute { get; set; }
    }
}
