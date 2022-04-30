
using UnityEngine;
using UnityEngine.U2D;
using System;
using Extensions;

namespace Modules.Cache
{
    public sealed class SpriteAtlasCache : IDisposable
    {
        //----- params -----

        //----- field -----

        private SpriteAtlas spriteAtlas = null;

        private Cache<Sprite> spriteCache = null;

		private bool disposed = false;

        //----- property -----

        public SpriteAtlas SpriteAtlas { get { return spriteAtlas; } }

        //----- method -----

        public SpriteAtlasCache(SpriteAtlas spriteAtlas, string referenceName = null)
        {
            this.spriteAtlas = spriteAtlas;

            spriteCache = new Cache<Sprite>(referenceName);
        }

		~SpriteAtlasCache()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (disposed) { return; }

			Clear();
			
			disposed = true;

			GC.SuppressFinalize(this);
		}

        public Sprite GetSprite(string spriteName)
        {
            if (spriteAtlas == null) { return null; }

            if (string.IsNullOrEmpty(spriteName)) { return null; }

            var sprite = spriteCache.Get(spriteName);

            if (sprite == null)
			{
	            sprite = spriteAtlas.GetSprite(spriteName);

				spriteCache.Add(spriteName, sprite);
			}

            return sprite;
        }

		public void Clear()
		{
			if (spriteCache != null)
			{
				var cahcedSprites = spriteCache.Values;

				foreach (var sprite in cahcedSprites)
				{
					UnityUtility.SafeDelete(sprite);
				}
			}
		}

        public bool HasCache(string spriteName)
        {
            return spriteCache.HasCache(spriteName);
        }
    }
}
