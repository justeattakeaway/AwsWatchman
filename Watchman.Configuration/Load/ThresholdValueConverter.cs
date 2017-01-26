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
                var simpleThreshold = (double)JToken.Load(reader);
                return new ThresholdValue(simpleThreshold, null);
            }

            var jsonObject = JObject.Load(reader);
            var thresholdProp = jsonObject["Threshold"];

            if (thresholdProp == null)
            {
                throw new JsonReaderException("Must be number or contain a 'Threshold' property");
            }

            var thresholdValue = thresholdProp.ToObject<double>();

            var evaluationPeriodsProp = jsonObject["EvaluationPeriods"];
            int? evaluationPeriods = null;
            if (evaluationPeriodsProp != null)
            {
                evaluationPeriods = evaluationPeriodsProp.ToObject<int>();
            }

            return new ThresholdValue(thresholdValue, evaluationPeriods);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
