using Watchman.Configuration;

namespace Watchman.Engine.Sns
{
    public class SnsCreator
    {
        private readonly ISnsTopicCreator _snsTopicCreator;
        private readonly ISnsSubscriptionCreator _snsSubscriptionCreator;

        public SnsCreator(ISnsTopicCreator snsTopicCreator, ISnsSubscriptionCreator snsSubscriptionCreator)
        {
            _snsTopicCreator = snsTopicCreator;
            _snsSubscriptionCreator = snsSubscriptionCreator;
        }

        public async Task<string> EnsureSnsTopic(AlertingGroup alertingGroup, bool dryRun)
        {
            var snsTopicArn = await _snsTopicCreator.EnsureSnsTopic(alertingGroup.Name, dryRun);

            if (!dryRun)
            {
                await _snsSubscriptionCreator.EnsureSnsSubscriptions(alertingGroup, snsTopicArn);
            }
            return snsTopicArn;
        }
    }
}
