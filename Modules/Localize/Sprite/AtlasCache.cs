
using UnityEngine;
using UnityEngine.U2D;
using Modules.Cache;

namespace Modules.Localize
{
    public sealed class AtlasCache
    {
        //----- params -----

        //----- field -----

		private SpriteAtlasCache spriteCache = null;

		//----- property -----

		public string AtlasPath { get; private set; }

		public int RefCount { get; private set; }

		public SpriteAtlas Atlas { get; private set; }

        //----- method -----

		public AtlasCache(string atlasPath, SpriteAtlas atlas)
		{
			AtlasPath = atlasPath;
			Atlas = atlas;

			spriteCache = new SpriteAtlasCache(atlas);
		}

		public void AddReference()
		{
			RefCount++;
		}

		public void ReleaseReference()
		{
			RefCount--;

			if (RefCount <= 0)
			{
				spriteCache.Clear();
			}
		}

		public Sprite GetSprite(string spriteName)
		{
			return spriteCache.GetSprite(spriteName);
		}
	}
}