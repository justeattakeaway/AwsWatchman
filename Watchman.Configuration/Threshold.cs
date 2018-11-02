namespace Watchman.Configuration
{
    public class Threshold
    {
        public ThresholdType ThresholdType { get; set; }
        public double Value { get; set; }
        public string SourceAttribute { get; set; }

        public Threshold CopyWith(double? value)
        {
            return new Threshold
            {
                SourceAttribute = SourceAttribute,
                ThresholdType = ThresholdType,
                Value = value ?? Value
            };
        }
    }
}
