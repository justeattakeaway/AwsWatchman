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
                var threshold = (double)JToken.Load(reader);
                return new ThresholdValue(threshold, null);
            }

            var jsonObject = JObject.Load(reader);
            var valueProp = jsonObject["Value"];

            if (valueProp == null)
            {
                throw new JsonReaderException("ThresholdValue must be number or contain a 'Value' property");
            }

            var value = valueProp.ToObject<double>();

            var evaluationPeriodsProp = jsonObject["EvaluationPeriods"];
            int? evaluationPeriods = null;
            if (evaluationPeriodsProp != null)
            {
                evaluationPeriods = evaluationPeriodsProp.ToObject<int>();
            }

            return new ThresholdValue(value, evaluationPeriods);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
