
using UnityEngine;
using UnityEditor;
using System.Reflection;
using Modules.Devkit.Prefs;

namespace Extensions.Devkit
{
    public static class EditorLayoutTools
    {
        //----- params -----

        #if UNITY_4_7 || UNITY_5_5 || UNITY_5_6
        
        public static string TextAreaStyle = "AS TextArea";
        
        #else
        
        public static string TextAreaStyle = "TextArea";
        
        #endif

        public struct IntRange
        {
            public int x;
            public int y;
        }

        //----- field -----

        private static Texture2D backdropTex = null;
        private static Texture2D contrastTex = null;

        private static MethodInfo drawSpriteMethod = null;

        //----- property -----

        private static Texture2D blankTexture
        {
            get { return EditorGUIUtility.whiteTexture; }
        }

        public static Texture2D backdropTexture
        {
            get
            {
                if (backdropTex == null)
                {
                    backdropTex = CreateCheckerTex(
                        new Color(0.1f, 0.1f, 0.1f, 0.5f),
                        new Color(0.2f, 0.2f, 0.2f, 0.5f));
                }

                return backdropTex;
            }
        }

        public static Texture2D contrastTexture
        {
            get
            {
                if (contrastTex == null)
                {
                    contrastTex = CreateCheckerTex(
                        new Color(0f, 0.0f, 0f, 0.5f),
                        new Color(1f, 1f, 1f, 0.5f));
                }

                return contrastTex;
            }
        }

        //----- method -----

        #region Field

        //===========================================
        // ObjectField.
        //===========================================

        public static T ObjectField<T>(T obj, bool allowSceneObjects, params GUILayoutOption[] options) where T : UnityEngine.Object
        {
            return EditorGUILayout.ObjectField(obj, typeof(T), allowSceneObjects, options) as T;
        }

        public static T ObjectField<T>(string label, T obj, bool allowSceneObjects, params GUILayoutOption[] options) where T : UnityEngine.Object
        {
            return EditorGUILayout.ObjectField(label, obj, typeof(T), allowSceneObjects, options) as T;
        }

        //===========================================
        // TextField.
        //===========================================

        public static string TextField(string label, string text, float space = 0f, params GUILayoutOption[] options)
        {
            return TextFieldInternal(label, text, false, space, options);
        }

        public static string DelayedTextField(string label, string text, float space = 0f, params GUILayoutOption[] options)
        {
            return TextFieldInternal(label, text, true, space, options);
        }

        private static string TextFieldInternal(string label, string text, bool delayed, float space = 0f, params GUILayoutOption[] options)
        {
            var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));

            var originLabelWidth = SetLabelWidth(textDimensions.x + space);

            var result = delayed ?
                EditorGUILayout.DelayedTextField(label, text, options) :
                EditorGUILayout.TextField(label, text, options);

            SetLabelWidth(originLabelWidth);

            return result;
        }

        //===========================================
        // IntField.
        //===========================================

        public static int IntField(string label, int value, float space = 0f, params GUILayoutOption[] options)
        {
            return IntFieldInternal(label, value, false, space, options);
        }

        public static int DelayedIntField(string label, int value, float space = 0f, params GUILayoutOption[] options)
        {
            return IntFieldInternal(label, value, true, space, options);
        }

        private static int IntFieldInternal(string label, int value, bool delayed, float space = 0f, params GUILayoutOption[] options)
        {
            var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));

            var originLabelWidth = SetLabelWidth(textDimensions.x + space);

            var result = delayed ? 
                EditorGUILayout.DelayedIntField(label, value, options) :
                EditorGUILayout.IntField(label, value, options);

            SetLabelWidth(originLabelWidth);

            return result;
        }

        public static IntRange IntRangeField(string prefix, string leftCaption, string rightCaption, int x, int y, bool editable = true)
        {
            return IntRangeFieldInternal(prefix, leftCaption, rightCaption, x, y, false, editable);
        }

        public static IntRange DelayedIntRangeField(string prefix, string leftCaption, string rightCaption, int x, int y, bool editable = true)
        {
            return IntRangeFieldInternal(prefix, leftCaption, rightCaption, x, y, true, editable);
        }

        private static IntRange IntRangeFieldInternal(string prefix, string leftCaption, string rightCaption, int x, int y, bool delayed, bool editable = true)
        {
            GUILayout.BeginHorizontal();

            if (string.IsNullOrEmpty(prefix))
            {
                GUILayout.Space(82f);
            }
            else
            {
                GUILayout.Label(prefix, GUILayout.Width(74f));
            }

            SetLabelWidth(48f);

            var retVal = new IntRange() { x = x, y = y };

            if (editable)
            {
                var backgroundColor = GUI.backgroundColor;

                GUI.backgroundColor = new Color(0f, 0.7f, 1f, 1f); // Blue.

                retVal.x = delayed ?
                    EditorGUILayout.DelayedIntField(leftCaption, x, GUILayout.MinWidth(30f)) :
                    EditorGUILayout.IntField(leftCaption, x, GUILayout.MinWidth(30f));

                retVal.y = delayed ?
                    EditorGUILayout.DelayedIntField(rightCaption, y, GUILayout.MinWidth(30f)) :
                    EditorGUILayout.IntField(rightCaption, y, GUILayout.MinWidth(30f));

                GUI.backgroundColor = backgroundColor;
            }
            else
            {
                GUILayout.Label(leftCaption + " ", GUILayout.Width(48f));
                GUILayout.TextArea(x.ToString(), GUILayout.MinWidth(30f));

                GUILayout.Label(rightCaption + " ", GUILayout.Width(48f));
                GUILayout.TextArea(y.ToString(), GUILayout.MinWidth(30f));
            }

            GUILayout.EndHorizontal();

            return retVal;
        }

        //===========================================
        // FloatField.
        //===========================================

        public static float FloatField(string label, float value, float space = 0f, params GUILayoutOption[] options)
        {
            return FloatFieldInternal(label, value, false, space, options);
        }

        public static float DelayedFloatField(string label, float value, float space = 0f, params GUILayoutOption[] options)
        {
            return FloatFieldInternal(label, value, true, space, options);
        }

        private static float FloatFieldInternal(string label, float value, bool delayed, float space = 0f, params GUILayoutOption[] options)
        {
            var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));

            var originLabelWidth = SetLabelWidth(textDimensions.x + space);

            var result = delayed ?
                EditorGUILayout.DelayedFloatField(label, value, options) :
                EditorGUILayout.FloatField(label, value, options);

            SetLabelWidth(originLabelWidth);

            return result;
        }

        //===========================================
        // BoolField.
        //===========================================

        public static bool BoolField(string label, bool state, float space = 0f, params GUILayoutOption[] options)
        {
            var textDimensions = GUI.skin.label.CalcSize(new GUIContent(label));

            var originLabelWidth = SetLabelWidth(textDimensions.x + space);

            var result = EditorGUILayout.Toggle(label, state, options);

            SetLabelWidth(originLabelWidth);

            return result;
        }

        #endregion

        public static void ColorLabel(string text, Color color, params GUILayoutOption[] options)
        {
            var oldColor = GUI.color;

            GUI.enabled = false;

            GUILayout.Label(text, options);

            GUI.enabled = true;

            GUI.color = oldColor;
        }

        public static bool ColorButton(string text, bool enabled, Color color, params GUILayoutOption[] options)
        {
            var oldEnabled = GUI.enabled;
            var oldColor = GUI.color;

            GUI.enabled = enabled;
            GUI.color = color;

            var ret = GUILayout.Button(text, options);

            GUI.enabled = oldEnabled;
            GUI.color = oldColor;

            return ret;
        }

        public static Object SelectAsset(string assetPath)
        {
            var instance = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));

            if (instance != null)
            {
                Selection.activeObject = instance;
            }

            return instance;
        }

        public static float SetLabelWidth(string text)
        {
            var textStyle = new GUIStyle();
            var textSize = textStyle.CalcSize(new GUIContent(text));

            return SetLabelWidth(textSize.x);
        }

        public static float SetLabelWidth(float width)
        {
            var prevWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = width;
            return prevWidth;
        }

        public static bool DrawPrefixButton(string text)
        {
            return GUILayout.Button(text, "DropDown", GUILayout.Width(76f));
        }

        public static bool DrawPrefixButton(string text, params GUILayoutOption[] options)
        {
            return GUILayout.Button(text, "DropDown", options);
        }

        public static bool DrawHeader(string text) { return DrawHeader(text, text); }

        public static bool DrawHeader(string text, string key, Color? color = null)
        {
            var state = ProjectPrefs.GetBool(key, true);

            GUI.changed = false;

            state = DrawHeader(text, state, color);

            if (GUI.changed) { ProjectPrefs.SetBool(key, state); }

            return state;
        }

        public static bool DrawHeader(string text, bool state, Color? color = null)
        {
            var c = color ?? Color.white;

            if (!state)
            {
                GUI.backgroundColor = new Color(c.r - 0.2f, c.g - 0.2f, c.b - 0.2f);
            }
            else
            {
                GUI.backgroundColor = c;
            }

            GUILayout.BeginHorizontal();

            text = "<b><size=11>" + text + "</size></b>";

            if (state)
            {
                text = "\u25BC " + text;
            }
            else
            {
                text = "\u25BA " + text;
            }

            if (!GUILayout.Toggle(true, text, "dragtab", GUILayout.MinWidth(20f)))
            {
                if (Event.current.button == 0)
                {
                    state = !state;
                }
            }

            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;

            if (!state) { GUILayout.Space(3f); }

            return state;
        }

        public class ColumnHeaderContent
        {
            public string text = string.Empty;
            public GUILayoutOption[] options = null;

            public ColumnHeaderContent(string text)
            {
                this.text = text;
            }

            public ColumnHeaderContent(string text, GUILayoutOption[] options = null) : this(text)
            {
                this.options = options;
            }

            public ColumnHeaderContent(string text, GUILayoutOption option) : this(text, new GUILayoutOption[] { option }) { }
        }

        public static void DrawColumnHeader(ColumnHeaderContent[] contents)
        {
            var style = new GUIStyle("ShurikenModuleTitle")
            {
                font = new GUIStyle(EditorStyles.label).font,
                border = new RectOffset(2, 2, 2, 2),
                fixedHeight = 17,
                contentOffset = new Vector2(0f, -2f),
                alignment = TextAnchor.MiddleCenter,
            };

            using (new EditorGUILayout.HorizontalScope())
            {
                foreach (var content in contents)
                {
                    var text = "<b><size=11>" + content.text + "</size></b>";

                    EditorGUILayout.LabelField(text, style, content.options);
                }         
            }
        }

        public static void DrawContentTitle(string text)
        {
            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                text = "<b><size=11>" + text + "</size></b>";

                GUILayout.Toggle(true, text, "dragtab", GUILayout.MinWidth(20f));

                GUILayout.Space(2f);
            }

            GUILayout.Space(-2f);
        }

        public static Color BackgroundColor
        {
            get { return EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.7f) : new Color(0f, 0f, 0f, 0.7f); }
        }

        public static Color LabelColor
        {
            get { return EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.7f); }
        }

        public static void DrawLabelWithBackground(string text, Color? backgroundColor = null, Color? labelColor = null, TextAnchor alignment = TextAnchor.MiddleLeft, float? width = null, params GUILayoutOption[] options)
        {
            var labelStyle = new GUIStyle("IN TextField");
            var labelStyleState = new GUIStyleState();

            backgroundColor = backgroundColor.HasValue ? backgroundColor.Value : BackgroundColor;
            labelColor = labelColor.HasValue ? labelColor.Value : LabelColor;

            labelStyle.alignment = alignment;
            labelStyleState.textColor = labelColor.Value;

            labelStyle.normal = labelStyleState;

            var style = new GUIStyle(TextAreaStyle);
            var size = labelStyle.CalcSize(new GUIContent(text));

            var originColor = GUI.backgroundColor;

            GUI.backgroundColor = backgroundColor.Value;

            var layoutOptions = width.HasValue ?
                new GUILayoutOption[] { GUILayout.Width(width.Value), GUILayout.Height(size.y) } :
                new GUILayoutOption[] { GUILayout.Height(size.y) };

            GUILayout.BeginHorizontal(style, layoutOptions);
            {
                GUILayout.Space(10f);

                GUILayout.Label(text, labelStyle, options);

                GUILayout.Space(10f);
            }
            GUILayout.EndHorizontal();

            GUI.backgroundColor = originColor;
        }

        public static void DrawOutline(Rect rect, Color color)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Texture2D tex = blankTexture;
                GUI.color = color;
                GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, 1f, rect.height), tex);
                GUI.DrawTexture(new Rect(rect.xMax, rect.yMin, 1f, rect.height), tex);
                GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, 1f), tex);
                GUI.DrawTexture(new Rect(rect.xMin, rect.yMax, rect.width, 1f), tex);
                GUI.color = Color.white;
            }
        }

        private static Texture2D CreateCheckerTex(Color c0, Color c1)
        {
            Texture2D tex = new Texture2D(16, 16);
            tex.name = "[Generated] Checker Texture";
            tex.hideFlags = HideFlags.DontSave;

            for (int y = 0; y < 8; ++y)
            {
                for (int x = 0; x < 8; ++x)
                {
                    tex.SetPixel(x, y, c1);
                }
            }

            for (int y = 8; y < 16; ++y)
            {
                for (int x = 0; x < 8; ++x)
                {
                    tex.SetPixel(x, y, c0);
                }
            }

            for (int y = 0; y < 8; ++y)
            {
                for (int x = 8; x < 16; ++x)
                {
                    tex.SetPixel(x, y, c0);
                }
            }

            for (int y = 8; y < 16; ++y)
            {
                for (int x = 8; x < 16; ++x)
                {
                    tex.SetPixel(x, y, c1);
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return tex;
        }

        public static void DrawTiledTexture(Rect rect, Texture tex)
        {
            GUI.BeginGroup(rect);
            {
                int width = Mathf.RoundToInt(rect.width);
                int height = Mathf.RoundToInt(rect.height);

                for (int y = 0; y < height; y += tex.height)
                {
                    for (int x = 0; x < width; x += tex.width)
                    {
                        GUI.DrawTexture(new Rect(x, y, tex.width, tex.height), tex);
                    }
                }
            }
            GUI.EndGroup();
        }

        public static void DrawTexture(Rect rect, float imageSize, Texture texture, Rect? uv = null)
        {
            DrawTiledTexture(rect, backdropTexture);

            if (texture != null && Event.current.type == EventType.Repaint)
            {
                if (!uv.HasValue)
                {
                    uv = new Rect(0f, 0f, 1f, 1f);
                }

                var scaleX = rect.width / uv.Value.width;
                var scaleY = rect.height / uv.Value.height;

                var aspect = (scaleY / scaleX) / ((float)texture.height / texture.width);

                if (aspect != 1f)
                {
                    if (aspect < 1f)
                    {
                        var padding = imageSize * (1f - aspect) * 0.5f;
                        rect.xMin += padding;
                        rect.xMax -= padding;
                    }
                    else
                    {
                        var padding = imageSize * (1f - 1f / aspect) * 0.5f;
                        rect.yMin += padding;
                        rect.yMax -= padding;
                    }
                }

                GUI.DrawTextureWithTexCoords(rect, texture, uv.Value);
            }
        }

        #region DrawSprite

        private static MethodInfo CreateDrawSpriteMethod()
        {
            if (drawSpriteMethod != null) { return drawSpriteMethod; }

            // ※ UnityEditor.UI.SpriteDrawUtility.DrawSpriteは公開されていないメソッドの為、リフレクションで実行.

            var type = System.Type.GetType("UnityEditor.UI.SpriteDrawUtility, UnityEditor.UI");

            var name = "DrawSprite";
            var bindingAttr = BindingFlags.NonPublic | BindingFlags.Static;
            var modifiers = new System.Type[]
            {
                typeof(Texture),
                typeof(Rect),
                typeof(Vector4),
                typeof(Rect),
                typeof(Rect),
                typeof(Rect),
                typeof(Color),
                typeof(Material)
            };

            return type.GetMethod(name, bindingAttr, null, modifiers, null);
        }

        /// <summary> Sprite描画 </summary>
        public static void DrawSprite(Sprite sprite, Rect drawArea, Vector4 border, Color color)
        {
            if (drawSpriteMethod == null)
            {
                drawSpriteMethod = CreateDrawSpriteMethod();
            }

            if (sprite == null) { return; }

            var tex = sprite.texture;

            if (tex == null) { return; }

            var outer = sprite.rect;
            var inner = outer;

            inner.xMin += border.x;
            inner.yMin += border.y;
            inner.xMax -= border.z;
            inner.yMax -= border.w;

            var uv4 = UnityEngine.Sprites.DataUtility.GetOuterUV(sprite);
            var uv = new Rect(uv4.x, uv4.y, uv4.z - uv4.x, uv4.w - uv4.y);
            var padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);

            padding.x /= outer.width;
            padding.y /= outer.height;
            padding.z /= outer.width;
            padding.w /= outer.height;

            drawSpriteMethod.Invoke(null, new object[] { tex, drawArea, padding, outer, inner, uv, color, null });
        }

        #endregion

        #region Inspector Preview

        public static void BeginContents()
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.BeginHorizontal(TextAreaStyle, GUILayout.MinHeight(10f));

            GUILayout.BeginVertical();
            GUILayout.Space(3f);
        }

        public static void EndContents()
        {
            GUILayout.Space(3f);
            GUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            GUILayout.EndHorizontal();
        }

        public static void DrawSprite(Rect rect, Sprite sprite, Color color, bool hasSizeLabel)
        {
            if (sprite == null) { return; }

            DrawSprite(sprite.texture, rect, color, null,
                Mathf.RoundToInt(sprite.textureRect.x),
                Mathf.RoundToInt(sprite.textureRect.y),
                Mathf.RoundToInt(sprite.textureRect.width),
                Mathf.RoundToInt(sprite.textureRect.height),
                Mathf.RoundToInt(sprite.border.x),
                Mathf.RoundToInt(sprite.border.y),
                Mathf.RoundToInt(sprite.border.z),
                Mathf.RoundToInt(sprite.border.w),
                hasSizeLabel
                );
        }

        public static void DrawSprite(Texture2D tex, Rect drawRect, Color color, Rect textureRect, Vector4 border, bool hasSizeLabel)
        {
            DrawSprite(tex, drawRect, color, null,
                Mathf.RoundToInt(textureRect.x),
                Mathf.RoundToInt(tex.height - textureRect.y - textureRect.height),
                Mathf.RoundToInt(textureRect.width),
                Mathf.RoundToInt(textureRect.height),
                Mathf.RoundToInt(border.x),
                Mathf.RoundToInt(border.y),
                Mathf.RoundToInt(border.z),
                Mathf.RoundToInt(border.w),
                hasSizeLabel);
        }

        public static void DrawSprite(Texture2D tex, Rect drawRect, Color color, Material mat,
            int x, int y, int width, int height, int borderLeft, int borderBottom, int borderRight, int borderTop, bool hasSizeLabel)
        {
            if (!tex) { return; }

            var outerRect = drawRect;

            outerRect.width = width;
            outerRect.height = height;

            //----- Label Area -----

            if(hasSizeLabel)
            {
                var text = string.Format("Size: {0}x{1}", Mathf.RoundToInt(width), Mathf.RoundToInt(height));

                var labelSize = GUI.skin.label.CalcSize(new GUIContent(text));

                drawRect.height -= labelSize.y;

                var labelRect = new Rect(drawRect.x, drawRect.y + drawRect.height, drawRect.width, labelSize.y);

                EditorGUI.DropShadowLabel(labelRect, text);
            }

            //----- Texture Area -----

            if (width > 0)
            {
                float f = drawRect.width / outerRect.width;
                outerRect.width *= f;
                outerRect.height *= f;
            }

            if (drawRect.height > outerRect.height)
            {
                outerRect.y += (drawRect.height - outerRect.height) * 0.5f;
            }
            else if (outerRect.height > drawRect.height)
            {
                float f = drawRect.height / outerRect.height;
                outerRect.width *= f;
                outerRect.height *= f;
            }

            if (drawRect.width > outerRect.width)
            {
                outerRect.x += (drawRect.width - outerRect.width) * 0.5f;
            }

            DrawTiledTexture(outerRect, backdropTexture);

            GUI.color = color;

            if (mat == null)
            {
                var uv = new Rect(x, y, width, height);
                uv = TextureUtility.ConvertToTexCoords(uv, tex.width, tex.height);
                GUI.DrawTextureWithTexCoords(outerRect, tex, uv, true);
            }
            else
            {
                EditorGUI.DrawPreviewTexture(outerRect, tex, mat);
            }

            GUI.BeginGroup(outerRect);
            {
                tex = contrastTexture;

                GUI.color = Color.white;

                if (borderLeft > 0)
                {
                    float x0 = (float)borderLeft / width * outerRect.width - 1;
                    DrawTiledTexture(new Rect(x0, 0f, 1f, outerRect.height), tex);
                }

                if (borderRight > 0)
                {
                    float x1 = (float)(width - borderRight) / width * outerRect.width - 1;
                    DrawTiledTexture(new Rect(x1, 0f, 1f, outerRect.height), tex);
                }

                if (borderBottom > 0)
                {
                    float y0 = (float)(height - borderBottom) / height * outerRect.height - 1;
                    DrawTiledTexture(new Rect(0f, y0, outerRect.width, 1f), tex);
                }

                if (borderTop > 0)
                {
                    float y1 = (float)borderTop / height * outerRect.height - 1;
                    DrawTiledTexture(new Rect(0f, y1, outerRect.width, 1f), tex);
                }
            }
            GUI.EndGroup();

            Handles.color = Color.black;
            Handles.DrawLine(new Vector3(outerRect.xMin, outerRect.yMin), new Vector3(outerRect.xMin, outerRect.yMax));
            Handles.DrawLine(new Vector3(outerRect.xMax, outerRect.yMin), new Vector3(outerRect.xMax, outerRect.yMax));
            Handles.DrawLine(new Vector3(outerRect.xMin, outerRect.yMin), new Vector3(outerRect.xMax, outerRect.yMin));
            Handles.DrawLine(new Vector3(outerRect.xMin, outerRect.yMax), new Vector3(outerRect.xMax, outerRect.yMax));
        }

        #endregion
    }
}
