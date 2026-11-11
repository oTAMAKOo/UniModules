
using UnityEngine;
using System.Collections;

namespace Extensions
{
    public static class MathUtility
    {
        public static float TwoPi = Mathf.PI * 2;
        public static float HalfPi = Mathf.PI / 2;
        public static float QuarterPi = Mathf.PI / 4;

        public static float DegsToRads(float degs)
        {
            return TwoPi * (degs / 360.0f);
        }

        public static bool InRange(float start, float end, float val)
        {
            if (start < end)
            {
                return (val > start) && (val < end);
            }
            else
            {
                return (val < start) && (val > end);
            }
        }

        public static float Sigmoid(float input, float response = 1.0f)
        {
            return (1.0f / (1.0f + Mathf.Exp(-input / response)));
        }
        
        /// <summary>
        /// 2点の角度を取得.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static float GetAngle(Vector2 p1, Vector2 p2)
        {
            var dx = p2.x - p1.x;
            var dy = p2.y - p1.y;
            var rad = Mathf.Atan2(dy, dx);

            return rad * Mathf.Rad2Deg;
        }
    }
}