
using System;﻿
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class RandomUtility
    {
        public static int Seed { get; private set; }

        public static Random Random { get; private set; }

        static RandomUtility()
        {
            SetRandomSeed();
        }

        public static void SetRandomSeed(int? seed = null)
        {
            Seed = seed.HasValue ? seed.Value : Environment.TickCount;

            if (Random == null)
            {
                Random = new Random(Seed);
            }
            else
            {
                lock (Random)
                {
                    Random = new Random(Seed);
                }
            }
        }

        private static int Range(int min, int max)
        {
            lock (Random)
            {
                return Random.Next(min, max);
            }
        }

        private static double Range(double min, double max)
        {
            lock (Random)
            {
                return min + (Random.NextDouble() * (max - min));
            }
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
            return (float)Range(float.MinValue, float.MaxValue);
        }

        public static float RandomInRange(float min, float max)
        {
            return (float)Range(min, max);
        }

        /// <summary> 0-max%を入力してヒットしたかを判定.</summary>
        public static bool IsPercentageHit(float percentage, float max = 100f)
        {
            return percentage != 0f && RandomInRange(0f, max) <= percentage;
        }

        //-----------------------------------------------
        // Random Type : Double.
        //-----------------------------------------------

        public static double RandomDouble()
        {
            return Range(double.MinValue, double.MaxValue);
        }

        public static double RandomInRange(double min, double max)
        {
            return Range(min, max);
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

            return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Next(s.Length)]).ToArray());
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
    }
}
