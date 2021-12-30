using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static partial class EnumerableExtensions
    {
        private static Random random = new Random();

        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return !source.Any();
        }

        public static string[] ToStrings(this object[] objectArray)
        {
            return Array.ConvertAll<object, string>(objectArray, o => o.ToString());
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> knownKeys = new HashSet<TKey>();

            foreach (TSource element in source)
            {
                if (knownKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static IEnumerable<T> Concat<T>(IEnumerable<IEnumerable<T>> source)
        {
            foreach (var item in source)
            {
                foreach (var item2 in item)
                {
                    yield return item2;
                }
            }
        }

        public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] source)
        {
            return Concat(source.AsEnumerable());
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
        {
            return new HashSet<T>(source);
        }

        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(source, comparer);
        }

        /// <summary>
        /// 重み付き抽選を実行し抽選されたインデックスを取得.
        /// 1000個入ったくじを([950, 49, 1]のような抽選される個数を元に抽選する)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int WeightSample(this IEnumerable<int> source)
        {
            var weightTable = source.ToArray();

            var totalWeight = weightTable.Sum();
            var value = random.Next(1, totalWeight + 1);

            var retIndex = -1;

            for (var i = 0; i < weightTable.Length; ++i)
            {
                if (weightTable[i] >= value)
                {
                    retIndex = i;
                    break;
                }

                value -= weightTable[i];
            }

            return retIndex;
        }

        /// <summary>
        /// 重み付き抽選を実行し抽選されたインデックスを取得.
        /// 1000個入ったくじを([950, 49, 1]のような抽選される個数を元に抽選する)
        /// </summary>
        public static T WeightSample<T>(this IEnumerable<T> source, Func<T, int> selector, T defaultValue = default(T))
        {
            var table = source.ToArray();

            var weightTable = table.Select(x => selector(x)).ToArray();

            var index = weightTable.WeightSample();

            if (index == -1) { return defaultValue; }

            return table[index];
        }

        /// <summary>
        /// 指定個数、ランダムで抽出します。同じものが複数回「重複して」でてくる可能性があります.
        /// 重複を避けたい場合はShuffleを使ってください.
        /// </summary>
        public static IEnumerable<T> Sample<T>(this IEnumerable<T> source, int sampleCount, Random random = null)
        {
            if (random == null)
            {
                random = EnumerableExtensions.random;
            }

            if (source == null) throw new ArgumentNullException("source");
            if (sampleCount <= 0) throw new ArgumentOutOfRangeException("sampleCount");

            return SampleCore(source, sampleCount, random);
        }

        private static IEnumerable<T> SampleCore<T>(this IEnumerable<T> source, int sampleCount, Random random)
        {
            var list = source as IList<T>;
            if (list == null)
            {
                list = source.ToList();
            }

            var len = list.Count;
            if (len == 0) yield break;

            for (int i = 0; i < sampleCount; i++)
            {
                var index = random.Next(0, len);
                yield return list[index];
            }
        }

        /// <summary>
        /// 値を一つだけランダムで抽出します.
        /// </summary>
        public static T SampleOne<T>(this IEnumerable<T> source, Random random = null, T defaultValue = default(T))
        {
            if (random == null)
            {
                random = EnumerableExtensions.random;
            }

            if (source == null) throw new ArgumentNullException("source");

            var array = source.ToArray();

            if (array.IsEmpty()) { return defaultValue; }

            if (array.Length == 1) { return array.First(); }

            return array.Sample(1, random).FirstOrDefault();
        }

        /// <summary>
        /// ランダムにシャッフルした順番で列挙します。ランダム生成子は渡されたものを用います。
        /// </summary>
        /// <param name="source">対象となる値のシーケンス</param>
        /// <param name="random">シャッフルに使用するランダム生成子</param>
        /// <returns>シャッフルされたシーケンス</returns>
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source, Random random = null)
        {
            if (random == null)
            {
                random = EnumerableExtensions.random;
            }

            if (source == null) throw new ArgumentNullException("source");

            return ShuffleCore(source, random);
        }

        private static IEnumerable<T> ShuffleCore<T>(this IEnumerable<T> source, Random random)
        {
            var buffer = source.ToArray();

            for (var i = buffer.Length - 1; i > 0; i--)
            {
                var j = random.Next(0, i + 1);

                yield return buffer[j];
                buffer[j] = buffer[i];
            }

            if (buffer.Length != 0)
            {
                yield return buffer[0];
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            var array = source as T[];
            if (array != null)
            {
                Array.ForEach(array, action);
                return;
            }

            var list = source as List<T>;
            if (list != null)
            {
                list.ForEach(action);
                return;
            }

            foreach (var item in source)
            {
                action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            var index = 0;
            foreach (var item in source)
            {
                action(item, index++);
            }
        }

        public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> action, int defaultValue = -1)
        {
            var index = 0;

            foreach (var item in source)
            {
                if (action(item))
                {
                    return index;
                }

                index++;
            }

            return defaultValue;
        }

        /// <summary>
        /// 要素のIndexを入れ替えます.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="firstIndex"></param>
        /// <param name="secondIndex"></param>
        /// <returns></returns>
        public static IEnumerable<T> Swap<T>(this IEnumerable<T> source, int firstIndex, int secondIndex)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            var array = source.ToArray();

            return Swap<T>(array, firstIndex, secondIndex);
        }

        private static IEnumerable<T> Swap<T>(IList<T> array, int firstIndex, int secondIndex)
        {
            if (firstIndex < 0 || firstIndex >= array.Count)
            {
                throw new ArgumentOutOfRangeException("firstIndex");
            }

            if (secondIndex < 0 || secondIndex >= array.Count)
            {
                throw new ArgumentOutOfRangeException("secondIndex");
            }

            var tmp = array[firstIndex];

            array[firstIndex] = array[secondIndex];
            array[secondIndex] = tmp;

            return array;
        }

        /// <summary>
        /// 指定したインデックス位置にある要素を返します.
        /// インデックスが範囲外の場合は既定値を返します.
        /// </summary>
        public static T ElementAtOrDefault<T>(this IEnumerable<T> source, int index, T defaultValue)
        {
            var array = source.ToArray();

            return index >= 0 && index < array.Length ? array[index] : defaultValue;
        }

        /// <summary>
        /// 最小値を持つ要素を返します.
        /// </summary>
        public static TSource FindMin<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            var array = source.ToArray();

            return array.FirstOrDefault(c => selector(c).Equals(array.Min(selector)));
        }

        /// <summary>
        /// 最大値を持つ要素を返します.
        /// </summary>
        public static TSource FindMax<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            var array = source.ToArray();

            return array.FirstOrDefault(c => selector(c).Equals(array.Max(selector)));
        }

        /// <summary>
        /// 複数のインデックスを削除.
        /// </summary>
        public static IEnumerable<T> RemoveAt<T>(this IEnumerable<T> source, IEnumerable<int> removeTargets)
        {
            var index = 0;
            var removeIndex = removeTargets.ToArray();

            var list = new List<T>();

            foreach (var item in source)
            {
                if (!removeIndex.Contains(index))
                {
                    list.Add(item);
                }

                index++;
            }

            return list;
        }

        /// <summary>
        /// 指定された個数ずつの要素に分割.
        /// </summary>
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> source, int chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentException("Chunk size must be greater than 0.", nameof(chunkSize));
            }

            return source.Select((v, i) => new { v, i })
                .GroupBy(x => x.i / chunkSize)
                .Select(g => g.Select(x => x.v));
        }

        /// <summary> 昇順・降順指定ソート. </summary>
        public static IOrderedEnumerable<T> Order<T, TKey>(this IEnumerable<T> source, bool ascending, Func<T, TKey> keySelector, IComparer<TKey> comparer = null)
        {
            return ascending ? source.OrderBy(keySelector, comparer) : source.OrderByDescending(keySelector, comparer);
        }
    }
}
