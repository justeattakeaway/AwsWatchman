namespace Watchman.Configuration.Validation
{
    public static class SqsValidation
    {
        public static void Validate(string alertingGroupName, Sqs sqs)
        {
            if (sqs.LengthThreshold.HasValue)
            {
                ValidQueueThreshold(sqs.LengthThreshold.Value);
            }

            if (sqs.OldestMessageThreshold.HasValue)
            {
                ValidQueueThreshold(sqs.OldestMessageThreshold.Value);
            }

            if (sqs.Errors != null)
            {
                if (sqs.Errors.LengthThreshold.HasValue)
                {
                    ValidQueueThreshold(sqs.Errors.LengthThreshold.Value);
                }

                if (sqs.Errors.OldestMessageThreshold.HasValue)
                {
                    ValidQueueThreshold(sqs.Errors.OldestMessageThreshold.Value);
                }
            }

            foreach (var queue in sqs.Queues)
            {
                ValidateQueue(alertingGroupName, queue);
            }
        }

        private static void ValidateQueue(string alertingGroupName, Queue queue)
        {
            if (queue == null)
            {
                throw new ConfigException($"AlertingGroup '{alertingGroupName}' has a null queue");
            }

            if (string.IsNullOrWhiteSpace(queue.Name) && string.IsNullOrWhiteSpace(queue.Pattern))
            {
                throw new ConfigException($"AlertingGroup '{alertingGroupName}' has a queue with no name or pattern");
            }

            if (!string.IsNullOrWhiteSpace(queue.Name) && !string.IsNullOrWhiteSpace(queue.Pattern))
            {
                throw new ConfigException($"AlertingGroup '{alertingGroupName}' has a queue '{queue.Name}' with a name and a pattern");
            }

            if (queue.LengthThreshold.HasValue)
            {
                ValidQueueThreshold(queue.LengthThreshold.Value);
            }

            if (queue.OldestMessageThreshold.HasValue)
            {
                ValidQueueThreshold(queue.OldestMessageThreshold.Value);
            }

            if (queue.Errors != null)
            {
                if (queue.Errors?.LengthThreshold != null)
                {
                    ValidQueueThreshold(queue.Errors.LengthThreshold.Value);
                }

                if (queue.Errors?.OldestMessageThreshold != null)
                {
                    ValidQueueThreshold(queue.Errors.OldestMessageThreshold.Value);
                }
            }
        }

        private static void ValidQueueThreshold(int threshold)
        {
            if (threshold <= 0)
            {
                throw new ConfigException($"Threshold of '{threshold}' must be greater than zero");
            }

            if (threshold > 100000)
            {
                throw new ConfigException($"Threshold of '{threshold}' is ridiculously high");
            }
        }
    }
}
