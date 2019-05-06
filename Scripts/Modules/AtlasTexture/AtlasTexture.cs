
using UnityEngine;
using UnityEngine.U2D;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Modules.ObjectCache;

namespace Modules.AtlasTexture
{
    public class AtlasTexture : ScriptableObject
    {
        //----- params -----

        [Serializable]
        public class AtlasSpriteData
        {
            [SerializeField, ReadOnly]
            private string spriteName = null;
            [SerializeField, ReadOnly]
            private string spriteGuid = null;

            public string SpriteName { get { return spriteName; } }
            public string SpriteGuid { get { return spriteGuid; } }

            public AtlasSpriteData(string spriteName, string spriteGuid)
            {
                this.spriteName = spriteName;
                this.spriteGuid = spriteGuid;
            }
        }

        //----- field -----

        [SerializeField]
        private SpriteAtlas spriteAtlas = null;
        [SerializeField]
        private AtlasSpriteData[] spriteData = null;

        private ObjectCache<Sprite> spriteCache = new ObjectCache<Sprite>();

        //----- property -----

        public AtlasSpriteData[] SpriteData { get { return spriteData; } }

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
            CacheClear();
        }

        /// <summary> Sprite取得. </summary>
        public Sprite GetSprite(string spriteName)
        {
            var sprite = spriteCache.Get(spriteName);

            if (sprite != null) { return sprite; }

            sprite = spriteAtlas.GetSprite(spriteName);

            if (sprite != null)
            {
                var cloneText = "(Clone)";

                if (sprite.name.EndsWith(cloneText))
                {
                    sprite.name = sprite.name.Substring(0, sprite.name.Length - cloneText.Length);
                }

                sprite.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

                spriteCache.Add(spriteName, sprite);
            }

            return sprite;
        }

        /// <summary> Sprite取得. </summary>
        public Sprite GetSpriteFromGuid(string spriteGuid)
        {
            var data = spriteData.FirstOrDefault(x => x.SpriteGuid == spriteGuid);

            if (data == null) { return null; }

            return GetSprite(data.SpriteName);
        }

        public void CacheClear()
        {
            if (spriteCache != null)
            {
                spriteCache.Clear();
            }
        }

        public AtlasSpriteData GetSpriteData(string spriteName)
        {
            return spriteData.FirstOrDefault(x => x.SpriteName == spriteName);
        }

        public AtlasSpriteData GetSpriteDataFromGuid(string spriteGuid)
        {
            return spriteData.FirstOrDefault(x => x.SpriteGuid == spriteGuid);
        }
    }
}
