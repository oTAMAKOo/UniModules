﻿﻿
using UnityEngine;
using System;
using System.Collections;

namespace Extensions
{
    public static class TextureUtility
    {
        /// <summary>
        /// 透過色で塗り潰したテクスチャを生成.
        /// </summary>
        public static Texture2D CreateEmptyTexture(int width, int height, TextureFormat format = TextureFormat.ARGB32)
        {
            var texture = new Texture2D(width, height, format, false);

            var colors = new Color32[width * height];

            for (var i = 0; i < colors.Length; i++)
            {
                colors[i] = Color.clear;
            }

            texture.SetPixels32(colors);

            texture.Apply(true, false);

            return texture;
        }

        /// <summary>
        /// テクスチャ座標をUV座標に変換.
        /// Convert from top-left based pixel coordinates to bottom-left based UV coordinates.
        /// </summary>
        public static Rect ConvertToTexCoords(Rect rect, int width, int height)
        {
            Rect final = rect;

            if (width != 0f && height != 0f)
            {
                final.xMin = rect.xMin / width;
                final.xMax = rect.xMax / width;
                final.yMin = 1f - rect.yMax / height;
                final.yMax = 1f - rect.yMin / height;
            }
            return final;
        }

        /// <summary>
        /// UV座標をテクスチャ座標に変換.
	    /// Convert from bottom-left based UV coordinates to top-left based pixel coordinates.
	    /// </summary>
        public static Rect ConvertToPixels(Rect rect, int width, int height, bool round)
        {
            Rect final = rect;

            if (round)
            {
                final.xMin = Mathf.RoundToInt(rect.xMin * width);
                final.xMax = Mathf.RoundToInt(rect.xMax * width);
                final.yMin = Mathf.RoundToInt((1f - rect.yMax) * height);
                final.yMax = Mathf.RoundToInt((1f - rect.yMin) * height);
            }
            else
            {
                final.xMin = rect.xMin * width;
                final.xMax = rect.xMax * width;
                final.yMin = (1f - rect.yMax) * height;
                final.yMax = (1f - rect.yMin) * height;
            }
            return final;
        }

        public static Texture2D CreateCheckerTex(Color c0, Color c1, int size = 16)
        {
            var tex = new Texture2D(size, size);
            tex.name = "[Generated] Checker Texture";
            tex.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

            for (var y = 0; y < size / 2 ; ++y)
            {
                for (var x = 0; x < size / 2; ++x)
                {
                    tex.SetPixel(x, y, c1);
                }
            }

            for (var y = size / 2; y < size; ++y)
            {
                for (var x = 0; x < size / 2; ++x)
                {
                    tex.SetPixel(x, y, c0);
                }
            }

            for (var y = 0; y < size / 2; ++y)
            {
                for (var x = size / 2; x < size; ++x)
                {
                    tex.SetPixel(x, y, c0);
                }
            }

            for (var y = size / 2; y < size; ++y)
            {
                for (var x = size / 2; x < size; ++x)
                {
                    tex.SetPixel(x, y, c1);
                }
            }

            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return tex;
        }

        /// <summary>
        /// pngファイルのバイト配列からTexture2Dを生成.
        /// </summary>
        public static Texture2D CreateTexture2DFromPngBytes(byte[] bytes)
        {
            if (bytes == null) { return null; }

            // 16バイトから開始.
            var pos = 16;

            var width = 0;

            for (var i = 0; i < 4; i++)
            {
                width = width * 256 + bytes[pos++];
            }

            var height = 0;
            for (var i = 0; i < 4; i++)
            {
                height = height * 256 + bytes[pos++];
            }

            var texture = new Texture2D(width, height);
            texture.LoadImage(bytes);

            return texture;
        }
    }
}
