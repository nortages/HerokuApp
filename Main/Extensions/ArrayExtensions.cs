using System;
using System.Collections.Generic;
using System.Linq;

namespace HerokuApp
{
    public static class ArrayExtensions
    {
        // The random number generator.
        private static readonly Random Rand = new Random();

        // Return a random item from an array.
        public static T RandomElement<T>(this T[] items)
        {
            if (items.Length == 0) return default;
            // Return a random item.
            return items[Rand.Next(0, items.Length)];
        }

        // Return a random item from a list.
        public static T RandomElement<T>(this List<T> items)
        {
            if (items.Count == 0) return default;
            // Return a random item.
            return items[Rand.Next(0, items.Count)];
        }

        // Return a random item from a list.
        public static T RandomElement<T>(this IEnumerable<T> items)
        {
            if (items.Count() == 0) return default;
            // Return a random item.
            return items.ElementAt(Rand.Next(0, items.Count()));
        }
    }

}
