﻿using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Moq;
using Watchman.Engine.Alarms;

namespace Watchman.Engine.Tests.Generation.Dynamo.Alarms
{
    public static class VerifyCloudwatch
    {
        public static void AlarmFinderFindsThreshold(Mock<IAlarmFinder> alarmFinder,
            double threshold, int period, string action)
        {
            alarmFinder.Setup(x => x.FindAlarmByName(It.IsAny<string>()))
                .ReturnsAsync(new MetricAlarm
                {
                    Threshold = threshold,
                    EvaluationPeriods = 1,
                    Period = period,
                    AlarmActions = new List<string> { action },
                    OKActions = new List<string> { action }
                });
        }

        public static void PutMetricAlarmWasCalledOnce(Mock<IAmazonCloudWatch> cloudWatch)
        {
            cloudWatch.Verify(x => x.PutMetricAlarmAsync(
                It.IsAny<PutMetricAlarmRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        public static void PutMetricAlarmWasNotCalled(Mock<IAmazonCloudWatch> cloudWatch)
        {
            cloudWatch.Verify(x => x.PutMetricAlarmAsync(
                It.IsAny<PutMetricAlarmRequest>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

    }
}
