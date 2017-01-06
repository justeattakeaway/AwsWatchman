namespace Watchman.Engine
{
    public static class AwsMetrics
    {
        public const string ConsumedWriteCapacity = "ConsumedWriteCapacityUnits";
        public const string ConsumedReadCapacity = "ConsumedReadCapacityUnits";

        public const string ReadThrottleEvents = "ReadThrottleEvents";
        public const string WriteThrottleEvents = "WriteThrottleEvents";

        public const string MessagesVisible = "ApproximateNumberOfMessagesVisible";
        public const string AgeOfOldestMessage = "ApproximateAgeOfOldestMessage";
    }
}
