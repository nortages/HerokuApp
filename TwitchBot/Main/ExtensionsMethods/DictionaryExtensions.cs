using System.Collections.Generic;

namespace TwitchBot.Main.ExtensionsMethods
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueAndSetIfNotExists<TKey, TValue>(this Dictionary<TKey, TValue> dictionary,
            TKey key,
            TValue defaultValue = default)
        {
            if (dictionary.ContainsKey(key)) return dictionary[key];

            dictionary[key] = defaultValue;
            return defaultValue;
        }
    }
}