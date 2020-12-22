
using UnityEngine;
using UnityEditor;
using System.Reflection;

namespace Extensions.Devkit
{
    public static partial class EditorLayoutTools
    {
        //----- params -----

        //----- field -----

        private static MethodInfo drawSpriteMethod = null;

        //----- property -----

        //----- method -----

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
                                      float x, float y, float width, float height,
                                      float borderLeft, float borderBottom, float borderRight, float borderTop,
                                      bool hasSizeLabel)
        {
            if (!tex) { return; }

            var outerRect = drawRect;

            outerRect.width = width;
            outerRect.height = height;

            //----- Label Area -----

            if (hasSizeLabel)
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
    }
}
