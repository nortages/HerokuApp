using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverter;

namespace HerokuApp
{
    internal class MethodNameToCallbackConverter<T> : NewtonsoftJsonConverter where T : EventArgs
    {
        public override bool CanConvert(Type objectType) => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var value = JToken.Load(reader);
            if (value.Type != JTokenType.String) return null;
            //objectType.GenericTypeArguments[0];
            string methodName = (string)value;
            Callback<T> callback = (args) => UtilityFunctions.CallMethodByName<string>(methodName, args);
            return callback;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
