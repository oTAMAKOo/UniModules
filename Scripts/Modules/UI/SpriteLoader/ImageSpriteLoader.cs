
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Cache;
using Modules.ExternalAssets;

namespace Modules.UI.SpriteLoader
{
    [RequireComponent(typeof(Image))]
    public sealed class ImageSpriteLoader : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private Image targetImage = null;

        private Cache<Sprite> spriteCache = null;

        private bool initialized = false;

        //----- property -----

        public string LoadPath { get; private set; }

        //----- method -----

        private void Initialize()
        {
            if (initialized){ return; }

            targetImage = UnityUtility.GetComponent<Image>(gameObject);

            spriteCache = new Cache<Sprite>(GetType().FullName + "-spriteCache");

            initialized = true;
        }

        public async UniTask SetSprite(string loadPath)
        {
            Initialize();

            if (LoadPath == loadPath){ return; }

            LoadPath = loadPath;

            targetImage.sprite = null;

            var sprite = spriteCache.Get(loadPath);
            
            if (sprite == null)
            {
                sprite = await ExternalAsset.LoadAsset<Sprite>(loadPath);
            }

            if (sprite != null)
            {
                spriteCache.Add(loadPath, sprite);

                targetImage.sprite = sprite;
            }

            UnityUtility.SetActive(targetImage, targetImage.sprite != null);
        }
    }
}