
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class Vector3Extensions
    {
        /// <summary>  Vector3を生成. </summary> 
        public static Vector3 ToVector3(this IEnumerable<float> src)
        {
            var array = src.ToArray();

            return new Vector3(array.ElementAtOrDefault(0), array.ElementAtOrDefault(1));
        }

        /// <summary> 
        /// Vector3のZ座標を無視してVector2に変換する 
        /// </summary> 
        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.y);
        }
    }
}
