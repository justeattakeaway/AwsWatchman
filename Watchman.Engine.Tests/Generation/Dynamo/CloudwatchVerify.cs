using System.Linq.Expressions;
using Amazon.CloudWatch;
using Amazon.CloudWatch.Model;
using Moq;

namespace Watchman.Engine.Tests.Generation.Dynamo
{
    public static class CloudwatchVerify
    {
        public static void AlarmWasPutMatching(Mock<IAmazonCloudWatch> cloudwatch,
            Expression<Func<PutMetricAlarmRequest, bool>> expression)
        {
            cloudwatch.Verify(x =>
                x.PutMetricAlarmAsync(It.Is(expression), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        public static void AlarmWasPutOnTable(Mock<IAmazonCloudWatch> cloudwatch,
            string alarmName, string tableName, string metricName)
        {
            AlarmWasPutMatching(cloudwatch,
                request =>
                request.AlarmName == alarmName
                && request.MetricName == metricName
                && IsForTable(request, tableName));
        }

        public static void AlarmWasPutOnTable(Mock<IAmazonCloudWatch> cloudwatch, string tableName, string metricName)
        {
            AlarmWasPutMatching(cloudwatch,
                request =>
                    request.MetricName == metricName
                    && IsForTable(request, tableName) && IsNotForIndex(request));
        }

        public static void AlarmWasPutOnTable(Mock<IAmazonCloudWatch> cloudwatch,
            string alarmName, string tableName, string metricName,
            int threshold, int period)
        {
            AlarmWasPutMatching(cloudwatch,
                request =>
                request.AlarmName == alarmName
                && request.MetricName == metricName
                && request.Statistic.Value == "Sum"
                && IsForTable(request, tableName)
                && request.EvaluationPeriods == 1
                && request.Period == period
                && request.Threshold.Equals(threshold)
                && request.ComparisonOperator == ComparisonOperator.GreaterThanOrEqualToThreshold
                && request.Namespace == "AWS/DynamoDB"
                && request.AlarmActions.Contains("sns-topic-arn")
                && request.OKActions.Contains("sns-topic-arn"));
        }

        public static void AlarmWasPutOnIndex(Mock<IAmazonCloudWatch> cloudwatch,
            string tableName, string indexName, string metricName)
        {
            AlarmWasPutMatching(cloudwatch,
                request =>
                    request.MetricName == metricName
                    && IsForTable(request, tableName)
                    && IsForIndex(request, indexName));
        }

        public static void AlarmWasPutOnIndex(Mock<IAmazonCloudWatch> cloudwatch,
            string alarmName, string tableName, string indexName, string metricName,
            int threshold, int period)
        {
            AlarmWasPutMatching(cloudwatch,
                request =>
                request.AlarmName == alarmName
                && request.MetricName == metricName
                && request.Statistic.Value == "Sum"
                && IsForTable(request, tableName)
                && IsForIndex(request, indexName)
                && request.EvaluationPeriods == 1
                && request.Period == period
                && request.Threshold.Equals(threshold)
                && request.ComparisonOperator == ComparisonOperator.GreaterThanOrEqualToThreshold
                && request.Namespace == "AWS/DynamoDB"
                && request.AlarmActions.Contains("sns-topic-arn")
                && request.OKActions.Contains("sns-topic-arn"));
        }

        public static void AlarmWasNotPutOnTable(Mock<IAmazonCloudWatch> cloudwatch, string tableName)
        {
            cloudwatch.Verify(x =>
                x.PutMetricAlarmAsync(It.Is<PutMetricAlarmRequest>(request =>
                request.Statistic.Value == "Sum"
                && IsForTable(request, tableName)
                && request.Namespace == "AWS/DynamoDB"), It.IsAny<CancellationToken>()), Times.Never);
        }

        public static void AlarmWasNotPutOnTable(Mock<IAmazonCloudWatch> cloudwatch,
            string tableName, string metricName)
        {
            cloudwatch.Verify(x =>
                x.PutMetricAlarmAsync(It.Is<PutMetricAlarmRequest>(request =>
                request.MetricName == metricName
                && request.Statistic.Value == "Sum"
                && IsForTable(request, tableName)
                && request.Namespace == "AWS/DynamoDB"), It.IsAny<CancellationToken>()), Times.Never);
        }

        public static void AlarmWasNotPutonIndex(Mock<IAmazonCloudWatch> cloudwatch,
            string tableName, string indexName)
        {
            cloudwatch.Verify(x =>
                x.PutMetricAlarmAsync(It.Is<PutMetricAlarmRequest>(request =>
                request.Statistic.Value == "Sum"
                && IsForTable(request, tableName)
                && IsForIndex(request, indexName)
                && request.Namespace == "AWS/DynamoDB"), It.IsAny<CancellationToken>()), Times.Never);
        }

        public static void AlarmWasNotPutOnIndex(Mock<IAmazonCloudWatch> cloudwatch,
            string tableName, string indexName, string metricName)
        {
            cloudwatch.Verify(x =>
                x.PutMetricAlarmAsync(It.Is<PutMetricAlarmRequest>(request =>
                    request.Statistic.Value == "Sum"
                    && IsForTable(request, tableName)
                    && IsForIndex(request, indexName)
                    && request.MetricName == metricName
                    && request.Namespace == "AWS/DynamoDB"), It.IsAny<CancellationToken>()), Times.Never);
        }

        public static void AlarmWasNotPutOnMetric(Mock<IAmazonCloudWatch> cloudwatch, string metric)
        {
            cloudwatch.Verify(x =>
                x.PutMetricAlarmAsync(It.Is<PutMetricAlarmRequest>(
                    request => request.MetricName == metric), It.IsAny<CancellationToken>()),
                    Times.Never);
        }

        private static bool IsForTable(PutMetricAlarmRequest r, string tableName)
        {
            return r.Dimensions.Count(x => x.Name == "TableName" && x.Value == tableName) == 1;
        }

        private static bool IsNotForIndex(PutMetricAlarmRequest r)
        {
            return !r.Dimensions.Any(x => x.Name == "GlobalSecondaryIndexName");
        }

        private static bool IsForIndex(PutMetricAlarmRequest r, string indexName)
        {
            return r.Dimensions.Count(x => x.Name == "GlobalSecondaryIndexName" && x.Value == indexName) == 1;
        }
    }
}
