
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.AdvKit.Standard
{
    public sealed class AdvBackground : AdvObject
    {
        #if ENABLE_MOONSHARP

        //----- params -----

        public const string UniqueIdentifier = "Background";

        //----- field -----

        [SerializeField]
        private Image image = null;
       

        //----- property -----

        //----- method -----

        protected override void OnInitialize()
        {
            SetPriority(int.MinValue);
        }

        public void Set(string fileName)
        {
            var advEngine = AdvEngine.Instance;

            var resourcePath = advEngine.Resource.GetResourcePath<AdvBackground>(fileName);

            if (string.IsNullOrEmpty(resourcePath)) { return; }

            image.sprite = advEngine.Resource.Get<Sprite>(resourcePath);

            var rectTransform = gameObject.transform as RectTransform;

            rectTransform.FillRect();

            UnityUtility.SetActive(gameObject, true);
        }

        public void Hide()
        {
            image.sprite = null;

            UnityUtility.SetActive(gameObject, false);
        }

        #endif
    }
}
