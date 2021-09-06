using System;
using System.Collections.Generic;
using System.Linq;
using TwitchBot.Models;

namespace TwitchBot.Main.ExtensionsMethods
{
    public static class ArrayExtensions
    {
        // The random number generator.
        private static readonly Random Rand = new();

        public static T RandomElement<T>(this T[] items)
        {
            return items.Length != 0 ? items[Rand.Next(0, items.Length)] : default;
        }

        public static T RandomElement<T>(this List<T> items)
        {
            return items.Count != 0 ? items[Rand.Next(0, items.Count)] : default;
        }

        // Return a random item from a list.
        public static T RandomElement<T>(this IEnumerable<T> items)
        {
            var enumerable = items as T[] ?? items.ToArray();
            return enumerable.Any() ? enumerable.ElementAt(Rand.Next(0, enumerable.Count())) : default;
            // Return a random item.
        }

        public static Option GetRandProbableOption(this IEnumerable<Option> probableOptions)
        {
            return GetProbableOption(probableOptions, Program.Rand.NextDouble());
        }
        
        public static Option GetProbableOption(this IEnumerable<Option> probableOptions, double randDouble)
        {
            var enabledOptions = probableOptions.Where(n => n.IsEnabled).ToArray();
            if (randDouble is > 1 or < 0)
            {
                throw new ArgumentException("Значение должно быть между 0 и 1");
            }

            Option result = null;
            var convertedProbabilities = GetConvertedProbabilities(enabledOptions);
            for (var i = 0; i < convertedProbabilities.Count; i++)
            {
                if (convertedProbabilities[i] < randDouble) continue;
                result = enabledOptions.ElementAt(i);
                break;
            }
            result ??= enabledOptions.Last();
            return result;
        }
        
        private static List<double> GetConvertedProbabilities(IReadOnlyCollection<Option> probableOptions)
        {
            var optionProbabilities = probableOptions.Select(o => o.Probability ?? 1.0 / probableOptions.Count);

            var sums = new List<double>();
            foreach (var probability in optionProbabilities)
            {
                var sum = sums.LastOrDefault();
                var newSum = sum + probability;
                sums.Add(newSum);
            }

            return sums;
        }
    }

}
