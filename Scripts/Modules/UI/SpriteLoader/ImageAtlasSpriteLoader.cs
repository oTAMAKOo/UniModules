
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.U2D;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Cache;
using Modules.ExternalAssets;

namespace Modules.UI.SpriteLoader
{
    [RequireComponent(typeof(Image))]
    public sealed class ImageAtlasSpriteLoader : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private Image targetImage = null;

        private Cache<SpriteAtlas> atlasCache = null;

        private SpriteAtlasCache spriteAtlasCache = null;

        private bool initialized = false;

        //----- property -----

        public string AtlasLoadPath { get; private set; }

        public string SpriteName { get; private set; }

        //----- method -----

        private void Initialize()
        {
            if (initialized){ return; }

            targetImage = UnityUtility.GetComponent<Image>(gameObject);

            atlasCache = new Cache<SpriteAtlas>(GetType().FullName + "-atlasCache");

            initialized = true;
        }

        public async UniTask SetSprite(string atlasLoadPath, string spriteName)
        {
            Initialize();

            if (spriteAtlasCache == null || AtlasLoadPath != atlasLoadPath)
            {
                var spriteAtlas = await ExternalAsset.LoadAsset<SpriteAtlas>(atlasLoadPath);

                if (spriteAtlas != null)
                {
                    AtlasLoadPath = atlasLoadPath;

                    atlasCache.Add(atlasLoadPath, spriteAtlas);

                    spriteAtlasCache = new SpriteAtlasCache(spriteAtlas, GetType().FullName + $"-{atlasLoadPath}_Cache");
                }
            }

            targetImage.sprite = null;

            var sprite = spriteAtlasCache.GetSprite(spriteName);
            
            if (sprite != null)
            {
                targetImage.sprite = sprite;
            }

            UnityUtility.SetActive(targetImage, targetImage.sprite != null);
        }
    }
}