
using UnityEngine;
using UnityEditor;
using Extensions;

namespace Extensions.Devkit
{
    public static partial class EditorLayoutTools
    {
        //----- params -----

        private static readonly Color LightSkinHeaderColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        private static readonly Color DarkSkinHeaderColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        private static readonly Color LightSkinContentColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color DarkSkinContentColor = new Color(1f, 1f, 1f, 1f);

        private static readonly Color LightBackgroundColor = new Color(0f, 0f, 0f, 0.7f);
        private static readonly Color DarkBackgroundColor = new Color(1f, 1f, 1f, 0.7f);

        private static readonly Color LightSkinLabelColor = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color DarkSkinLabelColor = new Color(1f, 1f, 1f, 0.9f);

        //----- field -----
        
        //----- property -----

        public static Color DefaultHeaderColor
        {
            get { return EditorGUIUtility.isProSkin ? DarkSkinHeaderColor : LightSkinHeaderColor; }
        }

        public static Color DefaultContentColor
        {
            get { return EditorGUIUtility.isProSkin ? DarkSkinContentColor : LightSkinContentColor; }
        }

        public static Color BackgroundColor
        {
            get { return EditorGUIUtility.isProSkin ? DarkBackgroundColor : LightBackgroundColor; }
        }

        public static Color LabelColor
        {
            get { return EditorGUIUtility.isProSkin ? DarkSkinLabelColor : LightSkinLabelColor; }
        }

        //----- method -----
        
        public static float SetLabelWidth(string text)
        {
            var calcLabelSizeStyle = new GUIStyle(EditorStyles.label);

            var textSize = calcLabelSizeStyle.CalcSize(new GUIContent(text));

            return SetLabelWidth(textSize.x);
        }

        public static float SetLabelWidth(float width)
        {
            var prevWidth = EditorGUIUtility.labelWidth;

            EditorGUIUtility.labelWidth = width;

            return prevWidth;
        }
    }
}
