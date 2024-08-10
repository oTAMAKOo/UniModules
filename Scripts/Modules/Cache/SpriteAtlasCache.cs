
using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Cache
{
    public sealed class SpriteAtlasCache : LifetimeDisposable
    {
        //----- params -----

        private class DeleteCacheObjectController
        {
            private bool initialized = false;

            private List<Sprite> targets = null;

            public void Initialize()
            {
                if (initialized){ return; }

                targets = new List<Sprite>();

                void DeleteSprites()
                {
                    lock (targets)
                    {
                        if (targets.IsEmpty()){ return; }

                        targets.ForEach(x => UnityUtility.SafeDelete(x));   

                        targets.Clear();
                    }
                }

                Observable.EveryEndOfFrame().Subscribe(_ => DeleteSprites());

                initialized = true;
            }

            public void DeleteRequest(Sprite sprite)
            {
                lock (targets)
                {
                    targets.Add(sprite);
                }
            }
        }

        //----- field -----

        private SpriteAtlas spriteAtlas = null;

        private Cache<Sprite> spriteCache = null;

        private static DeleteCacheObjectController deleteCacheObjectController = null;

        //----- property -----

        public SpriteAtlas SpriteAtlas { get { return spriteAtlas; } }

        //----- method -----

        public SpriteAtlasCache(SpriteAtlas spriteAtlas, string referenceName = null)
        {
            this.spriteAtlas = spriteAtlas;

            spriteCache = new Cache<Sprite>(referenceName);

            if (deleteCacheObjectController == null)
            {
                deleteCacheObjectController = new DeleteCacheObjectController();

                deleteCacheObjectController.Initialize();
            }
        }

		protected override void OnDispose()
		{
			Clear();
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
                    deleteCacheObjectController.DeleteRequest(sprite);
                }
			}
		}

        public bool HasCache(string spriteName)
        {
            return spriteCache.HasCache(spriteName);
        }
    }
}
