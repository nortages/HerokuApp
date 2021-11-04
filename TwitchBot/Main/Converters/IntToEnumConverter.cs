using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TwitchBot.Main.Converters
{
    internal class IntToEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var value = JToken.Load(reader);
            if (value.Type == JTokenType.Integer)
            {
                var newVal = Enum.Parse(objectType, value.ToString());
                return newVal;
            }

            if (value.Type == JTokenType.Null)
            {
                var newVal = Activator.CreateInstance(objectType);
                return newVal;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((int) value);
        }
    }
}