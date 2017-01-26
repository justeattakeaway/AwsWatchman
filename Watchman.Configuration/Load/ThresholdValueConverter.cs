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

                return new ThresholdValue(threshold, 1); // todo: latter should be nullable and null here
            }

            var jsonObject = JObject.Load(reader);
            double value = 0;
            var valueProp = jsonObject["Value"];

            if (valueProp == null)
            {
                value = jsonObject.ToObject<double>();
                return new ThresholdValue(value, 1);
            }

            value = valueProp.ToObject<double>();

            var evaluationPeriodsProp = jsonObject["EvaluationPeriods"];
            int? evaluationPeriods = null;
            if (evaluationPeriodsProp != null)
            {
                evaluationPeriods = evaluationPeriodsProp.ToObject<int>();
            }

            if (evaluationPeriods.HasValue)
            {
                return new ThresholdValue(value, evaluationPeriods.Value);
            }

            return new ThresholdValue(value, 1);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
