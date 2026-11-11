
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Extensions
{
    public static class ScreenUtility
    {
        public static Vector2Int GetSize()
        {
            var screenWidth = 0;
            var screenHeight = 0;

            #if UNITY_EDITOR

            var res = UnityStats.screenRes.Split('x');

            screenWidth = int.Parse(res[0]);
            screenHeight = int.Parse(res[1]);

            #else

            screenWidth = Screen.width;
            screenHeight = Screen.height;

            #endif

            return new Vector2Int(screenWidth, screenHeight);
        }
    }
}
