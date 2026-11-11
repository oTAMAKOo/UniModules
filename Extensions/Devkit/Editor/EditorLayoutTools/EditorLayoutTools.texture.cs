
using UnityEngine;
using UnityEditor;

namespace Extensions.Devkit
{
    public static partial class EditorLayoutTools
    {
        //----- params -----

        //----- field -----

        private static Texture2D backdropTex = null;
        private static Texture2D contrastTex = null;

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
                    backdropTex = CreateCheckerTexture(
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
                    contrastTex = CreateCheckerTexture(
                        new Color(0f, 0.0f, 0f, 0.5f),
                        new Color(1f, 1f, 1f, 0.5f));
                }

                return contrastTex;
            }
        }

        //----- method -----

        private static Texture2D CreateCheckerTexture(Color c0, Color c1)
        {
            var tex = new Texture2D(16, 16)
            {
                name = "[Generated] Checker Texture",
                hideFlags = HideFlags.DontSave,
            };

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
    }
}
