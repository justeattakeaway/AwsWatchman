namespace Watchman.Configuration
{
    public class ErrorQueue
    {
        public ErrorQueue()
        {
        }

        public ErrorQueue(ErrorQueue defaults)
        {
            if (defaults != null)
            {
                ReadDefaults(defaults);
            }
        }

        public bool? Monitored { get; set; }
        public string Suffix { get; set; }
        public int? LengthThreshold { get; set; }
        public int? OldestMessageThreshold { get; set; }

        public void ReadDefaults(ErrorQueue defaults)
        {
            Monitored = Monitored ?? defaults.Monitored;
            LengthThreshold = LengthThreshold ?? defaults.LengthThreshold;
            OldestMessageThreshold = OldestMessageThreshold ?? defaults.OldestMessageThreshold;

            if (string.IsNullOrWhiteSpace(Suffix) && !string.IsNullOrWhiteSpace(defaults.Suffix))
            {
                Suffix = defaults.Suffix;
            }
        }
    }
}
