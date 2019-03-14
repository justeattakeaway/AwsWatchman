using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Watchman.Configuration.Generic;

namespace Watchman.Configuration.Load
{
    class ResourceThresholdsConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        private static object CreateInstance(JsonReader reader, Type objectType, JsonSerializer serializer)
        {
            // serializer.Deserialize() would cause infinite loop
            var instance = Activator.CreateInstance(objectType);
            serializer.Populate(reader, instance);
            return instance;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType != JsonToken.StartObject)
            {
                return ReadSimpleValue(reader, objectType, serializer);
            }

            return CreateInstance(reader, objectType, serializer);
        }
        
        private static object ReadSimpleValue(JsonReader reader, Type objectType, JsonSerializer serializer)
        {
            var name = (string)JToken.Load(reader);

            var input = new JObject();
            input.Add("Name", name);

            using (var r = input.CreateReader())
            {
                return CreateInstance(r, objectType, serializer);
            }
        }

        private static object ReadStructuredValue(JsonReader reader, Type objectType)
        {
            var jsonObject = JObject.Load(reader);
            var result = jsonObject.ToObject(objectType);
            return result;
        }

        public override bool CanConvert(Type objectType)
        {
            if (!objectType.IsGenericType)
            {
                return false;
            }

            var canConvert = objectType.GetGenericTypeDefinition() == typeof(ResourceThresholds<>);

            return canConvert;
        }
    }
}
