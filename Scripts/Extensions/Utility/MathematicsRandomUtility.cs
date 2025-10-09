
using System;﻿
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    /// <summary> 
    /// 再現性のあるランダム生成
    /// ※ マルチスレッドで呼び出した場合、再現性はなくなります
    /// </summary>
    public static class MathematicsRandomUtility
    {
        private static Unity.Mathematics.Random random;

        public static uint Seed { get; private set; }

        /// <summary>
        /// 乱数シードを設定します。
        /// null の場合は TickCount を元に自動生成
        /// </summary>
        public static void SetRandomSeed(uint? seed = null)
        {
            Seed = seed.HasValue ? seed.Value : (uint)Environment.TickCount;

            random = new Unity.Mathematics.Random(Seed);
        }

        private static int Range(int min, int max)
        {
            return random.NextInt(min, max);
        }

        private static float Range(float min, float max)
        {
            return random.NextFloat(min, max);
        }

        //-----------------------------------------------
        // Random Type : Int.
        //-----------------------------------------------

        public static int RandomInt()
        {
            return Range(int.MinValue, int.MaxValue);
        }

        public static int RandomInRange(int min, int max)
        {
            return Range(min, max + 1);
        }

        /// <summary> 0-max%を入力してヒットしたかを判定.</summary>
        public static bool IsPercentageHit(int percentage, int max = 100)
        {
            return percentage != 0 && RandomInRange(1, max) <= percentage;
        }

        //-----------------------------------------------
        // Random Type : Float.
        //-----------------------------------------------

        public static float RandomFloat()
        {
            return Range(float.MinValue, float.MaxValue);
        }

        public static float RandomInRange(float min, float max)
        {
            return Range(min, max);
        }

        /// <summary> 0-max%を入力してヒットしたかを判定.</summary>
        public static bool IsPercentageHit(float percentage, float max = 100f)
        {
            return percentage != 0f && RandomInRange(0f, max) <= percentage;
        }

        //-----------------------------------------------
        // Random Type : Bool.
        //-----------------------------------------------

        public static bool RandomBool()
        {
            return (RandomInt() % 2) == 0;
        }

        //-----------------------------------------------
        // Random Type : String.
        //-----------------------------------------------

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var result = new char[length];

            for (var i = 0; i < length; i++)
            {
                var index = random.NextInt(0, chars.Length);

                result[i] = chars[index];
            }

            return new string(result);
        }

        //-----------------------------------------------
        // Random Type : Weight.
        //-----------------------------------------------

        /// <summary> 重みを考慮したランダム抽選 </summary>
        public static int GetRandomIndexByWeight(int[] weightTable)
        {
            var totalWeight = weightTable.Sum();
            var value = RandomInRange(1, totalWeight);
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

        /// <summary> 重みを考慮したランダム抽選 </summary>
        public static T GetRandomByWeight<T>(int[] weightTable, T[] valueTable)
        {
            if (weightTable.Length != valueTable.Length)
            {
                throw new ArgumentException();
            }

            var index = GetRandomIndexByWeight(weightTable);

            return valueTable[index];
        }

        //-----------------------------------------------
        // Sample (重複あり)
        //-----------------------------------------------

        public static IEnumerable<T> Sample<T>(IEnumerable<T> source, int sampleCount)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sampleCount <= 0) throw new ArgumentOutOfRangeException(nameof(sampleCount));

            var list = source as IList<T> ?? source.ToList();
            var len = list.Count;

            if (len == 0) yield break;

            for (var i = 0; i < sampleCount; i++)
            {
                var index = random.NextInt(0, len);

                yield return list[index];
            }
        }

        public static T SampleOne<T>(IEnumerable<T> source, T defaultValue = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var array = source.ToArray();

            if (array.Length == 0) return defaultValue;
            if (array.Length == 1) return array[0];

            return Sample(array, 1).FirstOrDefault();
        }

        //-----------------------------------------------
        // Shuffle
        //-----------------------------------------------

        public static IEnumerable<T> Shuffle<T>(IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var buffer = source.ToArray();

            for (var i = buffer.Length - 1; i > 0; i--)
            {
                var j = random.NextInt(0, i + 1);

                // swap
                var temp = buffer[i];
                buffer[i] = buffer[j];
                buffer[j] = temp;
            }

            foreach (var item in buffer)
            {
                yield return item;
            }
        }
    }
}
