namespace Watchman.Configuration.Validation
{
    public static class SqsValidation
    {
        public static void Validate(string alertingGroupName, Sqs sqs)
        {
            if (sqs.LengthThreshold.HasValue)
            {
                ValidQueueLength(sqs.LengthThreshold.Value);
            }

            if (sqs.OldestMessageThreshold.HasValue)
            {
                ValidQueueMaxAge(sqs.OldestMessageThreshold.Value);
            }

            if (sqs.Errors != null)
            {
                if (sqs.Errors.LengthThreshold.HasValue)
                {
                    ValidQueueLength(sqs.Errors.LengthThreshold.Value);
                }

                if (sqs.Errors.OldestMessageThreshold.HasValue)
                {
                    ValidQueueMaxAge(sqs.Errors.OldestMessageThreshold.Value);
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
                ValidQueueLength(queue.LengthThreshold.Value);
            }

            if (queue.OldestMessageThreshold.HasValue)
            {
                ValidQueueMaxAge(queue.OldestMessageThreshold.Value);
            }

            if (queue.Errors != null)
            {
                if (queue.Errors?.LengthThreshold != null)
                {
                    ValidQueueLength(queue.Errors.LengthThreshold.Value);
                }

                if (queue.Errors?.OldestMessageThreshold != null)
                {
                    ValidQueueMaxAge(queue.Errors.OldestMessageThreshold.Value);
                }
            }
        }

        private static void ValidQueueLength(int length)
        {
            if (length <= 0)
            {
                throw new ConfigException($"Queue length of '{length}' must be greater than zero");
            }

            if (length > 10000)
            {
                throw new ConfigException($"Queue length of '{length}' is ridiculously high");
            }
        }

        private static void ValidQueueMaxAge(int maxAge)
        {
            const int sevenDays = 60 * 60 * 24 * 7;

            if (maxAge <= 0)
            {
                throw new ConfigException($"Max age of '{maxAge}' must be greater than zero");
            }

            if (maxAge > sevenDays)
            {
                throw new ConfigException($"Max age of '{maxAge}' is ridiculously high");
            }
        }
    }
}
