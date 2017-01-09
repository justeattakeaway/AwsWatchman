using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Watchman.Engine.Generation.Generic
{
    static class CloudWatchCloudFormationTemplate
    {
        private static readonly Regex NonAlpha = new Regex("[^a-zA-Z0-9]+");


        public static string WriteJson(IList<Alarm> alarms)
        {
            var root = new JObject();
            root["AWSTemplateFormatVersion"] = "2010-09-09";

            var resources = new JObject();
            root["Resources"] = resources;

            foreach (var alarm in alarms)
            {
                var resourceName = NonAlpha.Replace(alarm.Resource.Name + alarm.AlarmDefinition.Name, "");
                var alarmJson = BuildAlarmJson(alarm);

                resources[resourceName] = alarmJson;
            }

            return root.ToString();
        }

        private static JObject BuildAlarmJson(Alarm alarm)
        {
            var alarmJson = new JObject();

            alarmJson["Type"] = "AWS::CloudWatch::Alarm";

            var definition = alarm.AlarmDefinition;


            var insufficientDataActions = definition.AlertOnInsufficientData
                ? new[] {alarm.SnsTopicArn}
                : Enumerable.Empty<string>();

            var properties = JObject.FromObject(new
            {
                AlarmDescription = "AwsWatchman alarm",
                AlarmName = alarm.AlarmName,
                Namespace = definition.Namespace,
                MetricName = definition.Metric,
                Dimensions = alarm.Dimensions
                    .Select(d => new
                    {
                        Name = d.Name,
                        Value = d.Value
                    }),
                AlarmActions = new[] {alarm.SnsTopicArn},
                OKActions = new [] { alarm.SnsTopicArn },
                InsufficientDataActions = insufficientDataActions,
                ComparisonOperator = definition.ComparisonOperator.Value,
                EvaluationPeriods = definition.EvaluationPeriods,
                Period = (int) definition.Period.TotalSeconds,
                Statistic = definition.Statistic.Value,
                Threshold = definition.Threshold.Value
            });

            alarmJson["Properties"] = properties;
            return alarmJson;
        }
    }
}
