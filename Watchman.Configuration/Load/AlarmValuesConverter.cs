using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Watchman.Configuration.Generic;

namespace Watchman.Configuration.Load
{
    public class AlarmValuesConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(AlarmValues).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return ReadSimpleValue(reader);
            }

            return ReadStructuredValue(reader);
        }
        private static AlarmValues ReadSimpleValue(JsonReader reader)
        {
            var token = JToken.Load(reader);

            switch (token.Type)
            {
                case JTokenType.Boolean:
                    return new AlarmValues(enabled: (bool) token);

                case JTokenType.Float:
                case JTokenType.Integer:
                    return (double) token;

                case JTokenType.String:
                    if (double.TryParse((string) token, out var result))
                    {
                        return result;
                    }

                    throw new JsonReaderException(
                        $"Invalid value {(string) token} (expected number) for path {reader.Path}");

                default:
                    throw new JsonReaderException($"Unexpected value of type {token.Type} for path {reader.Path}");
            }
        }

        private static AlarmValues ReadStructuredValue(JsonReader reader)
        {
            var jsonObject = JObject.Load(reader);
            var thresholdProp = jsonObject["Threshold"];
            var evalPeriodsProp = jsonObject["EvaluationPeriods"];
            var statistic = jsonObject["Statistic"];
            var extendedStatistic = jsonObject["ExtendedStatistic"];
            var enabled = jsonObject["Enabled"];
            var periodMinutes = jsonObject["PeriodMinutes"];

            if (thresholdProp == null && evalPeriodsProp == null && extendedStatistic == null && enabled == null && statistic == null)
            {
                throw new JsonReaderException("Must be number or contain a 'Threshold', 'EvaluationPeriods', 'ExtendedStatistic' or 'Enabled' property");
            }

            double? thresholdValue = null;
            int? evalPeriods = null;

            if (thresholdProp != null)
            {
                thresholdValue = thresholdProp.ToObject<double>();
            }

            if (evalPeriodsProp != null)
            {
                evalPeriods = evalPeriodsProp.ToObject<int>();
            }

            return new AlarmValues(thresholdValue,
                evalPeriods,
                statistic?.ToString(),
                extendedStatistic?.ToString(),
                enabled?.ToObject<bool>(),
                periodMinutes?.ToObject<int>());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
