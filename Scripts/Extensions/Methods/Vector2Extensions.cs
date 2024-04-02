
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class Vector2Extensions
    {
        /// <summary>  Vector2を生成. </summary> 
        public static Vector2 ToVector2(this IEnumerable<float> src)
        {
            var array = src.ToArray();

            return new Vector2(array.ElementAtOrDefault(0), array.ElementAtOrDefault(1));
        }

        /// <summary> ベクトル外積. </summary>
        public static float Cross(this Vector2 lhs, Vector2 rhs)
        {
            return lhs.x * rhs.y - rhs.x * lhs.y;
        }
        
        /// <summary> ベクトルの法線. </summary>
        public static Vector2 Perp(this Vector2 src)
        {
            return new Vector2(-src.y, src.x);
        }
        
        /// <summary> 点間距離. </summary>
        public static float Distance(this Vector2 lhs, Vector2 rhs)
        {
            return Mathf.Pow((rhs.x - lhs.x) * (rhs.x - lhs.x) + (rhs.y - lhs.y) * (rhs.y - lhs.y), 0.5f);
        }

        /// <summary> 対象座標との角度を取得. </summary>
        public static float Degrees(this Vector2 src, Vector2 vec)
        {
            var dx = vec.x - src.x;
            var dy = vec.y - src.y;

            var rad = Mathf.Atan2(dy, dx);

            return rad * Mathf.Rad2Deg;
        }

        public static Vector2 RotateAroundOrigin(this Vector2 src, float angle)
        {
            var x = src.x;
            var y = src.y;

            src.x = x * Mathf.Cos(angle) - y * Mathf.Sin(angle);
            src.y = x * Mathf.Sin(angle) + y * Mathf.Cos(angle);

            return src;
        }

        /// <summary>
        /// returns the length of a 2D vector.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static float Length(this Vector2 src)
        {
            return Mathf.Sqrt(src.x * src.x + src.y * src.y);
        }

        /// <summary>
        /// returns the squared length of a 2D vector.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static float LengthSq(this Vector2 src)
        {
            return (src.x * src.x + src.y * src.y);
        }

        /// <summary>
        /// returns positive if v2 is clockwise of this vector,
        ///  minus if anticlockwise (Y axis pointing down, X axis to right)
        /// </summary>
        /// <param name="src"></param>
        /// <param name="vec"></param>
        /// <returns></returns>
        public static int Sign(this Vector2 src, Vector2 vec)
        {
            return src.y * vec.x > src.x * vec.y ? 1 : -1;
        }

        /// <summary>
        /// given a normalized vector this method reflects the vector it
        /// is operating upon. (like the path of a ball bouncing off a wall)
        /// </summary>
        /// <param name="src"></param>
        /// <param name="norm"></param>
        public static void Reflect(this Vector2 src, Vector2 norm)
        {
            src += 2.0f * Vector2.Dot(src, norm) * norm.GetReverse();
        }

        /// <summary>
        /// returns the vector that is the reverse of this vector.
        /// </summary>
        /// <param name="src"></param>
        /// <returns></returns>
        public static Vector2 GetReverse(this Vector2 src)
        {
            return new Vector2(-src.x, -src.y);
        }

        /// <summary> 
        /// Vector2をVector3に変換. 
        /// </summary> 
        public static Vector3 ToVector3(this Vector2 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }

        /// <summary> 
        /// Vector2をZ座標0のVector3に変換.
        /// </summary> 
        public static Vector3 ToVector3(this Vector2 vector)
        {
            return vector.ToVector3(0);
        }
    }
}
