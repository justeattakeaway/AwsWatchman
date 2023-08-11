using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using NSubstitute;
using Watchman.Engine.Alarms;

namespace Watchman.Engine.Tests.Generation.Dynamo.Alarms
{
    public static class VerifyCloudwatch
    {
        public static void AlarmFinderFindsThreshold(IAlarmFinder alarmFinder,
            double threshold, int period, string action)
        {
            alarmFinder.FindAlarmByName(Arg.Any<string>())
                .Returns(new MetricAlarm
                {
                    Threshold = threshold,
                    EvaluationPeriods = 1,
                    Period = period,
                    AlarmActions = new List<string> { action },
                    OKActions = new List<string> { action }
                });
        }

        public static void PutMetricAlarmWasCalledOnce(IAmazonCloudWatch cloudWatch)
        {
            cloudWatch.Received(1).PutMetricAlarmAsync(
                Arg.Any<PutMetricAlarmRequest>(), Arg.Any<CancellationToken>());
        }

        public static void PutMetricAlarmWasNotCalled(IAmazonCloudWatch cloudWatch)
        {
            cloudWatch.DidNotReceive().PutMetricAlarmAsync(
                Arg.Any<PutMetricAlarmRequest>(), Arg.Any<CancellationToken>());
        }

    }
}
