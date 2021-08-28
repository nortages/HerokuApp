using System;
using System.Collections.Generic;
using System.Linq;

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

        public static OptionInfo GetRandProbableOption(this IEnumerable<OptionInfo> probableOptions)
        {
            return GetProbableOption(probableOptions, Program.Rand.NextDouble());
        }
        
        public static OptionInfo GetProbableOption(this IEnumerable<OptionInfo> probableOptions, double randDouble)
        {
            var enabledOptions = probableOptions.Where(n => n.IsEnabled).ToArray();
            if (randDouble is > 1 or < 0)
            {
                throw new ArgumentException("Значение должно быть между 0 и 1");
            }

            OptionInfo result = null;
            var convertedProbabilities = GetConvertedProbabilities(enabledOptions);
            for (var i = 0; i < convertedProbabilities.Count; i++)
            {
                if (!(convertedProbabilities[i] >= randDouble)) continue;
                result = enabledOptions.ElementAt(i);
                break;
            }
            result ??= enabledOptions.Last();
            return result;
        }
        
        private static List<double> GetConvertedProbabilities(IReadOnlyCollection<OptionInfo> probableOptions)
        {
            // If all options don't have the probability, assigns each of them equal one.
            if (probableOptions.All(n => n.Probability == null))
            {
                probableOptions.ToList().ForEach(n => n.Probability = 1.0 / probableOptions.Count());
            }

            var sums = new List<double>();
            foreach (var option in probableOptions)
            {
                var sum = sums.LastOrDefault();
                if (option.Probability == null)
                {
                    throw new ArgumentException($"One of the options has null probability");
                }
                var newSum = sum + (double)option.Probability;
                sums.Add(newSum);
            }

            return sums;
        }
    }

}
