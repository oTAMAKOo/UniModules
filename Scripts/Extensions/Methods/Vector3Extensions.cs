
using UnityEngine;

namespace Extensions
{
    public static class Vector3Extensions
    {
        /// <summary> 
        /// Vector3のZ座標を無視してVector2に変換する 
        /// </summary> 
        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.y);
        }
    }
}
