
using UnityEngine;

namespace Extensions
{
    public static class RectExtensions
    {
        public static bool Contains(this Rect self, Rect target) 
        {
            return self.Contains(new Vector2(target.xMin, target.yMax)) && self.Contains(new Vector2(target.xMax, target.yMin));
        }

        public static void DrawGizmos(this Rect rect)
        {
            Gizmos.DrawLine(new Vector3(rect.xMin, rect.yMin, 0f), new Vector3(rect.xMin, rect.yMax, 0f));  // 左下 -> 左上.
            Gizmos.DrawLine(new Vector3(rect.xMin, rect.yMax, 0f), new Vector3(rect.xMax, rect.yMax, 0f));  // 左上 -> 右上.
            Gizmos.DrawLine(new Vector3(rect.xMax, rect.yMax, 0f), new Vector3(rect.xMax, rect.yMin, 0f));  // 右上 -> 右下.
            Gizmos.DrawLine(new Vector3(rect.xMax, rect.yMin, 0f), new Vector3(rect.xMin, rect.yMin, 0f));  // 右下 -> 左下.
        }
    }
}
