﻿﻿﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Modules.ObjectCache;
using UniRx;

namespace Modules.Atlas
{
    [Serializable]
    public class AtlasTexture : ScriptableObject
    {
        //----- params -----

        //----- field -----

        [SerializeField, HideInInspector]
        private Texture texture = null;
        [SerializeField, HideInInspector]
        private List<SpriteData> sprites = new List<SpriteData>();
        [SerializeField, HideInInspector]
        private int padding = 2;
        [SerializeField, HideInInspector]
        private float pixelsPerUnit = 100f;
        [SerializeField, HideInInspector]
        private FilterMode filterMode = FilterMode.Bilinear;
        [SerializeField, HideInInspector]
        private bool unityPacking = false;
        [SerializeField, HideInInspector]
        private bool forceSquare = true;

        private ObjectCache<Sprite> spriteCache = null;

        //----- property -----

        public Texture Texture { get { return texture; } }

        public List<SpriteData> Sprites
        {
            get { return sprites; }
        }

        public int Padding
        {
            get { return padding; }
            set { padding = value; }
        }

        public float PixelsPerUnit
        {
            get { return pixelsPerUnit; }
            set { pixelsPerUnit = value; }
        }

        public FilterMode FilterMode
        {
            get { return filterMode; }
            set { filterMode = value; }
        }

        public bool UnityPacking
        {
            get { return unityPacking; }
            set { unityPacking = value; }
        }

        public bool ForceSquare
        {
            get { return forceSquare; }
            set { forceSquare = value; }
        }

        //----- method -----

        void OnEnable()
        {
            if (spriteCache == null)
            {
                spriteCache = new ObjectCache<Sprite>();
            }
        }

        void OnDisable()
        {
            if(spriteCache != null)
            {
                spriteCache.Clear();
            }
        }

        public SpriteData GetSpriteData(string name)
        {
            SpriteData data = null;

            // ※GUIDで検索して一致しなかったらspriteNameで検索.

            data = sprites.FirstOrDefault(x => !string.IsNullOrEmpty(x.guid) && x.guid == name);

            if (data == null)
            {
                data = sprites.FirstOrDefault(x => !string.IsNullOrEmpty(x.name) && x.name == name);
            }

            if (data != null && Texture != null)
            {
                var rect = ConvertToSpriteSheetPixels(
                    new Rect(new Vector2(data.x, data.y), new Vector2(data.width, data.height)),
                    Texture.width,
                    Texture.height
                    );

                data.u0 = rect.xMin / Texture.width;
                data.v0 = rect.yMax / Texture.height;
                data.u1 = rect.xMax / Texture.width;
                data.v1 = rect.yMin / Texture.height;
            }

            return data;
        }

        public Rect GetSpriteUV(string name, bool textureCoords = false)
        {
            var texture = Texture as Texture2D;
            var data = GetSpriteData(name);

            var rect = new Rect(new Vector2(data.x, data.y), new Vector2(data.width, data.height));

            if (!textureCoords)
            {
                rect = TextureUtility.ConvertToTexCoords(rect, texture.width, texture.height);
            }

            return rect;
        }

        public Sprite GetSprite(string name)
        {
            Sprite sprite = null;

            var texture = Texture as Texture2D;
            var data = GetSpriteData(name);

            if (texture != null && data != null)
            {
                sprite = spriteCache.Get(name);

                if(sprite == null)
                {
                    var rect = ConvertToSpriteSheetPixels(
                        new Rect(new Vector2(data.x, data.y), new Vector2(data.width, data.height)),
                        texture.width,
                        texture.height
                        );

                    var border = data.HasBorder ? new Vector4(data.borderLeft, data.borderBottom, data.borderRight, data.borderTop) : Vector4.zero;

                    var pivot = new Vector2(0.5f, 0.5f);

                    sprite = Sprite.Create(texture, rect, pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
                    sprite.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

                    spriteCache.Add(name, sprite);
                }
                else
                {
                    Debug.LogFormat("Get sprite from cache. : {0}", name);
                }
            }

            return sprite;
        }

        public void CacheClear()
        {
            if(spriteCache != null)
            {
                spriteCache.Clear();
            }
        }

        public void SortAlphabetically()
        {
            sprites.Sort((s1, s2) => string.Compare(s1.name, s2.name, StringComparison.Ordinal));
        }

        public string[] GetListOfSprites()
        {
            var list = new List<string>();

            for (int i = 0, imax = sprites.Count; i < imax; ++i)
            {
                var s = sprites[i];

                if (s != null && !string.IsNullOrEmpty(s.name)) list.Add(s.name);
            }
            return list.ToArray();
        }

        public string[] GetListOfSprites(string match)
        {
            if (string.IsNullOrEmpty(match)) return GetListOfSprites();

            var list = new List<string>();

            for (int i = 0, imax = sprites.Count; i < imax; ++i)
            {
                var sd = sprites[i];

                if (sd != null && !string.IsNullOrEmpty(sd.name) && string.Equals(match, sd.name, StringComparison.OrdinalIgnoreCase))
                {
                    list.Add(sd.name);
                    return list.ToArray();
                }
            }

            var keywords = match.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i) keywords[i] = keywords[i].ToLower();

            for (int i = 0, imax = sprites.Count; i < imax; ++i)
            {
                var sd = sprites[i];

                if (sd != null && !string.IsNullOrEmpty(sd.name))
                {
                    string tl = sd.name.ToLower();
                    int matches = 0;

                    for (int b = 0; b < keywords.Length; ++b)
                    {
                        if (tl.Contains(keywords[b])) ++matches;
                    }
                    if (matches == keywords.Length) list.Add(sd.name);
                }
            }
            return list.ToArray();
        }

        public static Rect ConvertToSpriteSheetPixels(Rect rect, float width, float height)
        {
            Rect final = rect;

            final.xMin = rect.xMin;
            final.xMax = rect.xMax;
            final.yMin = height - rect.yMax;
            final.yMax = height - rect.yMin;

            return final;
        }
    }
}