
using UnityEngine;

namespace Extensions
{
    /// <summary> ベジェ曲線 </summary>
    public class BezierCurve
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public Vector3[] ControlPoints { get; set; }

        //----- method -----

        public BezierCurve()
        {
            ControlPoints = null;
        }

        public BezierCurve(Vector3[] controlPoints)
        {
            ControlPoints = controlPoints;
        }

        /// <summary>
        /// 指定時間時のベジェ曲線上での座標を取得.
        /// </summary>
        /// <param name="time"> 0 - 1 までの時間</param>
        /// <returns></returns>
        public Vector3 Evaluate(float time)
        {
            if (ControlPoints == null) { return Vector3.zero; }

            var result = Vector3.zero;
            var n = ControlPoints.Length;

            for (var i = 0; i < n; i++)
            {
                result += ControlPoints[i] * Bernstein(n - 1, i, time);
            }

            return result;
        }

        // バーンスタイン基底関数.
        private static float Bernstein(int n, int i, float t)
        {
            return Binomial(n, i) * Mathf.Pow(t, i) * Mathf.Pow(1 - t, n - i);
        }

        // 二項係数を計算.
        private static float Binomial(int n, int k)
        {
            return Factorial(n) / (Factorial(k) * Factorial(n - k));
        }

        // 階乗を計算.
        private static float Factorial(int a)
        {
            var result = 1f;

            for (var i = 2; i <= a; i++)
            {
                result *= i;
            }

            return result;
        }
    }
}
