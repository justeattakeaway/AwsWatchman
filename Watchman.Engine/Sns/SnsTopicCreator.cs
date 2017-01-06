using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Watchman.Engine.Logging;

namespace Watchman.Engine.Sns
{
    public class SnsTopicCreator : ISnsTopicCreator
    {
        private readonly IAmazonSimpleNotificationService _snsClient;
        private readonly IAlarmLogger _logger;

        public SnsTopicCreator(IAmazonSimpleNotificationService snsClient,
            IAlarmLogger logger)
        {
            _snsClient = snsClient;
            _logger = logger;
        }

        public async Task<string> EnsureSnsTopic(string alertingGroupName, bool dryRun)
        {
            var topicName = alertingGroupName + "-Alerts";
            var topic = await _snsClient.FindTopicAsync(topicName);

            if (topic != null)
            {
                _logger.Detail($"Found SNS topic {topicName} with ARN {topic.TopicArn}");
                return topic.TopicArn;
            }

            if (dryRun)
            {
                _logger.Info($"Skipped: Created SNS topic {topicName}");
                return topicName;
            }

            var createResponse = await _snsClient.CreateTopicAsync(topicName);
            _logger.Info($"Created SNS topic {topicName} with ARN {createResponse.TopicArn}");
            return createResponse.TopicArn;
        }
    }
}
