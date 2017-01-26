using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Watchman.Configuration.Generic;

namespace Watchman.Configuration.Load
{
    public class ThresholdValueConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ThresholdValue).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return ReadSimpleThresholdValue(reader);
            }

            return ReadStructuredThresholdValue(reader);
        }
        private static object ReadSimpleThresholdValue(JsonReader reader)
        {
            var simpleThreshold = (double)JToken.Load(reader);
            return new ThresholdValue(simpleThreshold, null);
        }

        private static object ReadStructuredThresholdValue(JsonReader reader)
        {
            var jsonObject = JObject.Load(reader);
            var thresholdProp = jsonObject["Threshold"];
            var evalPeriodsProp = jsonObject["EvaluationPeriods"];

            if ((thresholdProp == null) && (evalPeriodsProp == null))
            {
                throw new JsonReaderException("Must be number or contain a 'Threshold' or 'EvaluationPeriods' property");
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

            return new ThresholdValue(thresholdValue, evalPeriods);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
