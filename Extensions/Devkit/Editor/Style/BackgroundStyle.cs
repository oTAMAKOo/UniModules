
using UnityEngine;

namespace Extensions.Devkit.Style
{
    public static class BackgroundStyle
    {
        private static GUIStyle style = new GUIStyle();
            
        private static Texture2D texture = new Texture2D(1, 1);
            
        public static GUIStyle Get(Color color)
        {
            texture.SetPixel(0, 0, color);
            texture.Apply();

            style.normal.background = texture;

            return style;
        }
    }
}