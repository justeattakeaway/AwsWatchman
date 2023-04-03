namespace Watchman.Configuration
{
    public class Sqs
    {
        public int? LengthThreshold { get; set; }

        public int? OldestMessageThreshold { get; set; }

        public ErrorQueue Errors { get; set; }

        public List<Queue> Queues { get; set; }
    }
}
