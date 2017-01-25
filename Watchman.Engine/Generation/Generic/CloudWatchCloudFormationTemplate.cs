using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using Watchman.Configuration;

namespace Watchman.Engine.Generation.Generic
{
    public class CloudWatchCloudFormationTemplate
    {
        private static readonly Regex NonAlpha = new Regex("[^a-zA-Z0-9]+");

        private readonly List<Alarm> _alarms = new List<Alarm>();

        private string _emailTopicResourceName = null;
        private string _urlTopicResourceName = null;

        public void AddAlarms(IEnumerable<Alarm> alarms)
        {
            _alarms.AddRange(alarms);
        }

        public string WriteJson()
        {
            var root = new JObject();
            root["AWSTemplateFormatVersion"] = "2010-09-09";

            var resources = new JObject();
            root["Resources"] = resources;

            AddSnsTopics(_alarms, resources);

            foreach (var alarm in _alarms)
            {
                var resourceName = NonAlpha.Replace(alarm.Resource.Name + alarm.AlarmDefinition.Name, "");
                var alarmJson = BuildAlarmJson(alarm);

                resources[resourceName] = alarmJson;
            }

            return root.ToString();
        }

        private static JObject CreateSnsTopic<T>(string description, List<T> targets, Func<T, JObject> mapper) where T : AlertTarget
        {
            var sns = JObject.FromObject(new
            {
                Type = "AWS::SNS::Topic",
                Properties = new
                {
                    DisplayName = description
                    //  TopicName todo: decide
                }
            });

            var subscriptions = targets.Select(mapper);
            sns["Properties"]["Subscription"] = new JArray(subscriptions);
            return sns;
        }

        private void AddSnsTopics(IEnumerable<Alarm> alarms, JObject resources)
        {
            // this is making an assumption that all alarms within an alerting group have the same targets. this is true and makes sense, but doesn't quite
            // seem right because in actual fact each alarm is supplying its own copy (the same) of the service-specific alerting group (which each defines the targets),
            // so the representation of the alarm that arrives here probably needs some future attention

            var targets = alarms
                .SelectMany(a => a.AlertingGroup.Targets ?? new List<AlertTarget>())
                .Distinct()
                .ToList();

            var emails = targets.OfType<AlertEmail>().ToList();

            if (emails.Any())
            {
                var sns = CreateSnsTopic(AwsConstants.DefaultEmailTopicDesciption, emails, email => JObject.FromObject(new
                {
                    Protocol = "email",
                    Endpoint = email.Email
                }));

                _emailTopicResourceName = "EmailTopic";

                resources[_emailTopicResourceName] = sns;
            }

            var urls = targets.OfType<AlertUrl>().ToList();
            if (urls.Any())
            {
                var sns = CreateSnsTopic(AwsConstants.DefaultUrlTopicDesciption, urls, url =>
                {
                    var protocol = url.Url.StartsWith("https") ? "https" : "http";
                    return JObject.FromObject(new
                    {
                        Protocol = protocol,
                        Endpoint = url.Url
                    });
                });

                _urlTopicResourceName = "UrlTopic";
                resources[_urlTopicResourceName] = sns;
            }
        }

        private JObject BuildAlarmJson(Alarm alarm)
        {
            var alarmJson = new JObject();
            alarmJson["Type"] = "AWS::CloudWatch::Alarm";
            alarmJson["Properties"] = BuildAlarmPropertiesJson(alarm);
            return alarmJson;
        }

        private JObject BuildAlarmPropertiesJson(Alarm alarm)
        {
            var definition = alarm.AlarmDefinition;

            var propsObject = new
            {
                AlarmName = alarm.AlarmName,
                AlarmDescription = AwsConstants.DefaultDescription,
                Namespace = definition.Namespace,
                MetricName = definition.Metric,
                Dimensions = alarm.Dimensions.Select(d => new { d.Name, d.Value }),
                ComparisonOperator = definition.ComparisonOperator.Value,
                EvaluationPeriods = definition.EvaluationPeriods,
                Period = (int) definition.Period.TotalSeconds,
                Statistic = definition.Statistic.Value,
                Threshold = definition.Threshold.Value
            };

            var result = JObject.FromObject(propsObject);

            if (definition.AlertOnInsufficientData)
            {
                result["InsufficientDataActions"] = TargetRefs(email: true, url: true);
            }

            if (definition.AlertOnOk)
            {
                result["OKActions"] = TargetRefs(email: false, url: true);
            }

            result["AlarmActions"] = TargetRefs(email: true, url: true);

            return result;
        }

        private JArray TargetRefs(bool email, bool url)
        {
            return new JArray(TargetResourceNames(email, url).Select(t => JObject.FromObject(new
            {
                Ref = t
            })));
        }

        private IEnumerable<string> TargetResourceNames(bool email, bool url)
        {
            if (email && !string.IsNullOrEmpty(_emailTopicResourceName))
            {
                yield return _emailTopicResourceName;
            }

            if (url && !string.IsNullOrEmpty(_urlTopicResourceName))
            {
                yield return _urlTopicResourceName;
            }
        }

        private static IEnumerable<string> ValueOrEmpty(bool hasValue, string value)
        {
            return hasValue ? new[] {value} : Enumerable.Empty<string>();
        }
    }
}
