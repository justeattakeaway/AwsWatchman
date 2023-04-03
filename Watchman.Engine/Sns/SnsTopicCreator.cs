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

            if (dryRun)
            {
                _logger.Info($"Skipped: Created SNS topic {topicName}");
                return topicName;
            }

            // https://docs.aws.amazon.com/sns/latest/api/API_CreateTopic.html
            // "This action is idempotent, so if the requester already owns a topic with the specified name,
            // that topic's ARN is returned without creating a new topic."
            var createResponse = await _snsClient.CreateTopicAsync(topicName);
            _logger.Info($"Created SNS topic {topicName} with ARN {createResponse.TopicArn}");
            return createResponse.TopicArn;
        }
    }
}
