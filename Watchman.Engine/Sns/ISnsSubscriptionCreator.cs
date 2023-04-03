using Watchman.Configuration;

namespace Watchman.Engine.Sns
{
    public interface ISnsSubscriptionCreator
    {
        Task EnsureSnsSubscriptions(AlertingGroup alertingGroup, string snsTopicArn);
        Task EnsureSnsSubscriptions(IEnumerable<AlertTarget> alertingTargets, string snsTopicArn);
    }
}
