using System;
using System.Web;
using System.Collections.Generic;

namespace TagRides.Shared.Utilities
{
    public static class MiscUtils
    {
        /// <summary>
        /// A thread-safe way of getting the value from a nullable type. Calling
        /// HasValue and then Value can be a race condition because another thread
        /// could set the value to null after the HasValue check.
        /// </summary>
        /// <returns><c>true</c>, if the nullable had a value, <c>false</c> otherwise.</returns>
        /// <param name="value">Set to the nullable's Value if it has one, and set to default otherwise.</param>
        public static bool TryGetValue<T>(this T? nullable, out T value) where T : struct
        {
            T? copy = nullable;

            if (copy.HasValue)
            {
                value = copy.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns a new Uri with the added query
        /// From: https://stackoverflow.com/questions/14517798/append-values-to-query-string
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="paramName"></param>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        public static Uri AddParameter(this Uri uri, string paramName, string paramValue)
        {
            UriBuilder builder = new UriBuilder(uri);
            var query = HttpUtility.ParseQueryString(builder.Query);

            query.Add(paramName, paramValue);
            builder.Query = query.ToString();

            return builder.Uri;
        }

        /// <summary>
        /// The same as SkipLast in System.Linq. Turns out SkipLast is not
        /// available in NETStandard.Library (2.0.3)
        /// </summary>
        public static IEnumerable<T> SkipLastAlt<T>(this IEnumerable<T> ls, int n)
        {
            var enumerator = ls.GetEnumerator();

            if (n == 0)
            {
                while (enumerator.MoveNext())
                    yield return enumerator.Current;
                yield break;
            }

            if (n < 0)
                throw new ArgumentException("Cannot skip a negative number of elements.");

            Queue<T> vals = new Queue<T>();
            for (int i = 0; i < n; ++i)
            {
                if (!enumerator.MoveNext())
                    throw new InvalidOperationException("Enumerable doesn't have enough values for SkipLastAlt");

                vals.Enqueue(enumerator.Current);
            }

            while (enumerator.MoveNext())
            {
                yield return vals.Dequeue();
                vals.Enqueue(enumerator.Current);
            }
        }

        /// <summary>
        /// Enumerates the consecutive pairs in the sequence. If the sequence does
        /// not have at least two elements, this will result in an empty sequence.
        /// </summary>
        /// <returns>The consecutive pairs in the sequence.</returns>
        /// <param name="ls">The sequence.</param>
        public static IEnumerable<(T, T)> ConsecutivePairs<T>(this IEnumerable<T> ls)
        {
            var enumerator = ls.GetEnumerator();

            if (!enumerator.MoveNext())
                yield break;

            T prev = enumerator.Current;

            while (enumerator.MoveNext())
            {
                T curr = enumerator.Current;
                yield return (prev, curr);
                prev = curr;
            }
        }
    }
}
