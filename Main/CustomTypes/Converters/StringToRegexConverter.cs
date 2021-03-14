using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverter;
using System.Text.RegularExpressions;

namespace HerokuApp
{
    internal class StringToRegexConverter : NewtonsoftJsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            const RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

            var value = JToken.Load(reader);
            if (value.Type != JTokenType.String) return null;
            return new Regex((string)value, regexOptions);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    internal class IntToEnumConverter : NewtonsoftJsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = JToken.Load(reader);
            if (value.Type == JTokenType.Integer)
            {
                var newVal = Enum.Parse(objectType, value.ToString());
                return newVal;
            }
            else if (value.Type == JTokenType.Null)
            {
                var newVal = Activator.CreateInstance(objectType);
                return newVal;
            }
            else
            {
                return null;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            writer.WriteValue((int)value);
        }
    }
}
