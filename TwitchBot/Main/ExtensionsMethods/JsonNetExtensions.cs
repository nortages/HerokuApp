using Newtonsoft.Json.Linq;

namespace TwitchBot.Main.ExtensionsMethods
{
    public static class JsonNetExtensions
    {
        public static JProperty GetJProperty(this JToken token)
        {
            return token.Parent as JProperty;
        }
    }
}