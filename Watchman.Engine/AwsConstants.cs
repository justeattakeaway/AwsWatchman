namespace Watchman.Engine
{
    public static class AwsConstants
    {
        public const int OneMinuteInSeconds = 60;
        public const int FiveMinutesInSeconds = OneMinuteInSeconds * 5;

        // Dynamo
        public const double DefaultCapacityThreshold = 0.8;
        public const double ThrottlingThreshold = 2.0;

        // SQS
        public const int QueueLengthThreshold = 100;
        public const int ErrorQueueLengthThreshold = 10;
        public const int OldestMessageThreshold = OneMinuteInSeconds * 10;
    }
}
