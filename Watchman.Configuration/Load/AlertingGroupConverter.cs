using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
                NumberOfCloudFormationStacks = (int)(jsonObject["NumberOfCloudFormationStacks"] ?? 1),
                Name = (string)jsonObject["Name"],
                Description = (string)jsonObject["Description"],
                ReportTargets = jsonObject["ReportTargets"]?.ToObject<List<ReportTarget>>(serializer) ?? new List<ReportTarget>(),
                Services = jsonObject["Services"]?.ToObject<AlertingGroupServices>(serializer)
            };

            if (jsonObject["Targets"] != null)
            {
                ReadIntoTargets(result, jsonObject["Targets"]);
            }

            ReadServiceDefinitions(jsonObject, result, serializer);

            return result;
        }

        private static void ReadServiceDefinitions(JObject jsonObject, AlertingGroup result, JsonSerializer serializer)
        {
            if (jsonObject["DynamoDb"] != null)
            {
                result.DynamoDb = jsonObject["DynamoDb"].ToObject<DynamoDb>(serializer);
            }

            if (jsonObject["Sqs"] != null)
            {
                result.Sqs = jsonObject["Sqs"].ToObject<Sqs>(serializer);
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
                    result.Targets.Add(new AlertEmail(item["Email"].ToString()));
                }
                else if (item["Url"] != null)
                {
                    result.Targets.Add(new AlertUrl(item["Url"].ToString()));
                }
                else
                {
                    _logger.Warn($"The target {jToken} is unknown. Valid targets are 'Email' and 'Url'.");
                }
            }
        }
    }
}
