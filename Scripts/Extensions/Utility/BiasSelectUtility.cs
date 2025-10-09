
using System;
using System.Linq;
using System.Collections.Generic;

namespace Extensions
{
    public static class BiasSelectUtility
    {
        private static Func<double> DefaultRandomFunc = () => RandomUtility.RandomFloat();

        private static Func<double> randomFunc = DefaultRandomFunc;

        public static void SetRandomFunc(Func<double> func)
        {
            if (func == null)
            {
                randomFunc = DefaultRandomFunc;
            }
            else
            {
                randomFunc = func;
            }
        }

        /// <summary> バイアス付きで1件を選択する </summary>
        public static T SelectOne<T>(IEnumerable<T> source, Func<T, double> scoreSelector, Func<double, double> weightFunc, double minWeight = 1e-6)
        {
            var items = source.ToList();

            if (items.Count == 0){ return default; }

            var weighted = items.Select(item =>
            {
                var score = scoreSelector(item);
                var weight = weightFunc(score);
                
                if (double.IsNaN(weight) || double.IsInfinity(weight))
                {
                    weight = minWeight;
                }

                return (item, weight: Math.Max(minWeight, weight));
            }).ToList();

            var total = weighted.Sum(x => x.weight);

            if (total <= 0)
            {
                // 完全ランダム.
                return items[randomFuncIndex(items.Count)];
            }

            var r = randomFunc() * total;
            var acc = 0d;

            foreach (var (item, weight) in weighted)
            {
                acc += weight;

                if (r <= acc){ return item; }
            }

            return weighted.Last().item;
        }

        /// <summary> バイアス確率判定 </summary>
        public static bool IsHit<T>(T target, Func<T, double> scoreSelector, Func<double, double> weightFunc)
        {
            var score = scoreSelector(target);
            var weight = Math.Clamp(weightFunc(score), 0.0, 1.0);

            return randomFunc() <= weight;
        }

        private static int randomFuncIndex(int max)
        {
            return (int)(randomFunc() * max);
        }

        //-----------------------------------------------
        // 重み関数プリセット
        //-----------------------------------------------

        /// <summary> スコアが小さいほどより選ばれやすくなる（強調度付き） </summary>
        public static double BiasLowPower(double ratio, double k = 2.0) => Math.Pow(1.0 - ratio, k);

        /// <summary> スコアが小さいほど極端に選ばれやすくなる（スコアが小さいほど大きく重みが増す） </summary>
        public static double BiasLowInverse(double ratio, double epsilon = 0.01) => 1.0 / (ratio + epsilon);

        /// <summary> スコアが大きいほどより選ばれやすくなる（強調度付き） </summary>
        public static double BiasHighPower(double ratio, double k = 2.0) => Math.Pow(ratio, k);

        /// <summary> スコアが小さいほどより選ばれにくくなる（強調度付き） </summary>
        public static double BiasLowDisfavorPower(double ratio, double k = 2.0) => Math.Pow(ratio, k);

        /// <summary> スコアが小さいほど極端に選ばれにくくなる（スコアが小さいとほぼ選ばれない） </summary>
        public static double BiasLowDisfavorInverse(double ratio, double epsilon = 0.01) => 1.0 - BiasLowInverse(ratio, epsilon);
    }
}