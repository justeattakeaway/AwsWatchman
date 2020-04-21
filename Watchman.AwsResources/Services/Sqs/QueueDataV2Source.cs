using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Watchman.AwsResources.Services.Sqs
{
    public class QueueDataV2Source : ResourceSourceBase<QueueDataV2>
    {
        private readonly QueueSource _queueSource;
        private const string ErrorQueueSuffix = "_error";

        public QueueDataV2Source(QueueSource queueSource)
        {
            _queueSource = queueSource;
        }

        protected override string GetResourceName(QueueDataV2 resource)
        {
            return resource.Name;
        }

        protected override async Task<IEnumerable<QueueDataV2>> FetchResources()
        {
            var queueNames = await _queueSource.GetResourceNamesAsync();

            var uniqResourcesNames = queueNames
                .Select(i => TrimEnd(i, ErrorQueueSuffix, StringComparison.OrdinalIgnoreCase))
                .Distinct();

            var result = new List<QueueDataV2>();
            foreach (var resourcesName in uniqResourcesNames)
            {
                var workingQueueName = resourcesName;
                var errorQueueName = $"{resourcesName}{ErrorQueueSuffix}";

                var hasWorkingQueue = queueNames.Any(i => string.Equals(workingQueueName, i));
                var hasErrorQueue = queueNames.Any(i => string.Equals(errorQueueName, i));

                var resource = new QueueDataV2(resourcesName);

                if (hasWorkingQueue)
                {
                    resource.SetWorkingQueue(workingQueueName);
                    resource.SetErrorQueue(errorQueueName);
                }

                if(!hasWorkingQueue && hasErrorQueue)
                    resource.SetErrorQueue(errorQueueName);

                result.Add(resource);
            }

            return result;
        }

        // TODO: extract into extensions
        public static string TrimEnd(string input, string suffixToRemove, StringComparison comparisonType)
        {
            if (input != null && suffixToRemove != null
                              && input.EndsWith(suffixToRemove, comparisonType))
            {
                return input.Substring(0, input.Length - suffixToRemove.Length);
            }

            return input;
        }
    }
}
