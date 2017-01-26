using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Watchman.Configuration.Generic;

namespace Watchman.Configuration.Load
{
    public class AlertingGroupConverter : JsonConverter
    {
        private readonly IConfigLoadLogger _logger;

        public AlertingGroupConverter(IConfigLoadLogger logger)
        {
            _logger = logger;
        }

        public override bool CanConvert(Type objectType)
        {
            return typeof(AlertingGroup).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jsonObject = JObject.Load(reader);
            var result = new AlertingGroup
            {
                AlarmNameSuffix = (string)jsonObject["AlarmNameSuffix"],
                IsCatchAll = (bool)(jsonObject["IsCatchAll"] ?? false),
                Name = (string)jsonObject["Name"],
                ReportTargets = jsonObject["ReportTargets"]?.ToObject<List<ReportTarget>>() ?? new List<ReportTarget>(),
                Services = new Dictionary<string, AwsServiceAlarms>()
            };

            if (jsonObject["Targets"] != null)
            {
                ReadIntoTargets(result, jsonObject["Targets"]);
            }

            ReadServiceDefinitions(jsonObject, result);

            return result;
        }

        private static void ReadServiceDefinitions(JObject jsonObject, AlertingGroup result)
        {
            var readDynamo = false;
            var readSqs = false;

            if (jsonObject["DynamoDb"] != null)
            {
                result.DynamoDb = jsonObject["DynamoDb"].ToObject<DynamoDb>();
                readDynamo = true;
            }

            if (jsonObject["Sqs"] != null)
            {
                result.Sqs = jsonObject["Sqs"].ToObject<Sqs>();
                readSqs = true;
            }

            var allServices = (JObject) jsonObject["Services"];
            if (allServices != null)
            {
                foreach (var prop in allServices)
                {
                    if (prop.Key == "DynamoDb")
                    {
                        if (readDynamo)
                        {
                            throw new JsonReaderException("DynamoDb block can only defined once");
                        }

                        result.DynamoDb = prop.Value.ToObject<DynamoDb>();
                    }
                    else if (prop.Key == "Sqs")
                    {
                        if (readSqs)
                        {
                            throw new JsonReaderException("Sqs block can only defined once");
                        }

                        result.Sqs = prop.Value.ToObject<Sqs>();
                    }
                    else
                    {
                        result.Services[prop.Key] = prop.Value.ToObject<AwsServiceAlarms>();
                    }
                }
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        private void ReadIntoTargets(AlertingGroup result, JToken jToken)
        {
            if (!jToken.HasValues)
            {
                return;
            }

            foreach (var item in jToken.Children())
            {
                if (item["Email"] != null)
                {
                    result.Targets.Add(new AlertEmail { Email = item["Email"].ToString() });
                }
                else if (item["Url"] != null)
                {
                    result.Targets.Add(new AlertUrl { Url = item["Url"].ToString() });
                }
                else
                {
                    _logger.Warn($"The target {jToken} is unknown. Valid targets are 'Email' and 'Url'.");
                }
            }
        }
    }
}
