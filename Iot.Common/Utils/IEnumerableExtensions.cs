using System;
using System.Collections.Generic;

namespace Iot.Common.Utils
{
    // ReSharper disable once InconsistentNaming
    public static class IEnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> ToBatches<T>(this IEnumerable<T> items, int partitionSize)
        {
            using (var enumerator = items.GetEnumerator())
                while (enumerator.MoveNext())
                    yield return YieldBatchElements(enumerator, partitionSize - 1);
        }
        private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> items, int partitionSize)
        {
            yield return items.Current;
            for (var i = 0; i < partitionSize && items.MoveNext(); i++)
            {
                yield return items.Current;
            }
        }
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var obj in source)
                action(obj);
        }

        public static IEnumerable<T> LazyDefaultIfEmpty<T>(this IEnumerable<T> source, Func<T> defaultFactory)
        {
            var isEmpty = true;

            foreach (var value in source)
            {
                yield return value;
                isEmpty = false;
            }

            if (isEmpty)
                yield return defaultFactory();

        }

    }
}
