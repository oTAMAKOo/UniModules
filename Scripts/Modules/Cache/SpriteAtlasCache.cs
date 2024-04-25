
using UnityEngine;
using UnityEngine.U2D;
using System;
using UniRx;
using Extensions;

namespace Modules.Cache
{
    public sealed class SpriteAtlasCache : LifetimeDisposable
    {
        //----- params -----

        //----- field -----

        private SpriteAtlas spriteAtlas = null;

        private Cache<Sprite> spriteCache = null;

        //----- property -----

        public SpriteAtlas SpriteAtlas { get { return spriteAtlas; } }

        //----- method -----

        public SpriteAtlasCache(SpriteAtlas spriteAtlas, string referenceName = null)
        {
            this.spriteAtlas = spriteAtlas;

            spriteCache = new Cache<Sprite>(referenceName);
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
                void DeleteCacheObjects()
                {
                    var cahcedSprites = spriteCache.Values;

                    foreach (var sprite in cahcedSprites)
                    {
                        UnityUtility.SafeDelete(sprite);
                    }
                }

                // メインスレッドで破棄.
                Observable.ReturnUnit()
                    .ObserveOnMainThread()
                    .Subscribe(_ => DeleteCacheObjects())
                    .AddTo(Disposable);
			}
		}

        public bool HasCache(string spriteName)
        {
            return spriteCache.HasCache(spriteName);
        }
    }
}
