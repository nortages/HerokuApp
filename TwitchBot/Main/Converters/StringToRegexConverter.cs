using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverter;

namespace TwitchBot.Main.Converters
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
            return value.Type == JTokenType.String ? new Regex(value.Value<string>(), regexOptions) : null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
