
using UnityEngine;
using UnityEngine.U2D;

namespace Modules.ObjectCache
{
    public sealed class SpriteAtlasCache
    {
        //----- params -----

        //----- field -----

        private SpriteAtlas spriteAtlas = null;

        private ObjectCache<Sprite> spriteCache = null;

        //----- property -----

        public SpriteAtlas SpriteAtlas { get { return spriteAtlas; } }

        //----- method -----

        public SpriteAtlasCache(SpriteAtlas spriteAtlas, string referenceName = null)
        {
            this.spriteAtlas = spriteAtlas;

            spriteCache = new ObjectCache<Sprite>(referenceName);
        }

        public Sprite GetSprite(string spriteName)
        {
            if (spriteAtlas == null) { return null; }

            if (string.IsNullOrEmpty(spriteName)) { return null; }

            var sprite = spriteCache.Get(spriteName);

            if (sprite != null) { return sprite; }

            sprite = spriteAtlas.GetSprite(spriteName);

            return sprite;
        }

        public bool HasCache(string spriteName)
        {
            return spriteCache.HasCache(spriteName);
        }
    }
}
