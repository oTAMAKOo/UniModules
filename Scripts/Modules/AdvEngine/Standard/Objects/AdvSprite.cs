
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.AdvKit.Standard
{
    public sealed class AdvSprite : AdvObject
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Image image = null;

        //----- property -----

        //----- method -----

        public void Show(string fileName, float? width, float? height)
        {
            var advEngine = AdvEngine.Instance;

            var resourcePath = advEngine.Resource.GetResourcePath<AdvSprite>(fileName);

            var sprite = advEngine.Resource.Get<Sprite>(resourcePath);

            var rt = transform as RectTransform;

            if (rt != null)
            {
                var size = new Vector2()
                {
                    x = width.HasValue ? width.Value : sprite.rect.width,
                    y = height.HasValue ? height.Value : sprite.rect.height,
                };

                rt.SetSize(size);
            }

            image.sprite = sprite;

            UnityUtility.SetActive(gameObject, true);
        }

        public void Hide()
        {
            UnityUtility.SetActive(gameObject, false);
        }
    }
}