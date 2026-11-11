
using UnityEngine;
using UnityEngine.U2D;
using System.Collections.Generic;
using R3;
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

                Observable.EveryUpdate(UnityFrameProvider.PostLateUpdate).Subscribe(_ => DeleteSprites());

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

            if (spriteCache != null)
            {
                spriteCache.Dispose();
                spriteCache = null;
            }
        }

        public Sprite GetSprite(string spriteName)
        {
            if (spriteAtlas == null){ return null; }

            if (string.IsNullOrEmpty(spriteName)){ return null; }

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
            if (spriteCache == null){ return; }

            var cachedSprites = spriteCache.Values;

            // 共有モードでは最終参照者でない限り実クリアされない. 実クリアされた時のみ Sprite を解放する.
            if (!spriteCache.Clear()){ return; }

            foreach (var sprite in cachedSprites)
            {
                deleteCacheObjectController.DeleteRequest(sprite);
            }
        }

        public bool HasCache(string spriteName)
        {
            return spriteCache.HasCache(spriteName);
        }
    }
}
