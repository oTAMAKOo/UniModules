
using Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UI.SpriteNumber
{
    public sealed class SpriteNumberImage : SpriteNumberBase<Image>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private bool raycastTarget = false;

        //----- property -----

        //----- method -----

        protected override void OnInitialize()
        {
            UnityUtility.GetOrAddComponent<Canvas>(gameObject);
        }

        public void ChangeRaycastTarget(bool raycastTarget)
        {
            this.raycastTarget = raycastTarget;

            Apply();
        }

        protected override void OnApply(Image component)
        {
            component.raycastTarget = raycastTarget;
        }

        protected override void UpdateComponent(Image component, Sprite sprite, Color color)
        {
            component.sprite = sprite;
            component.color = color;

            component.SetNativeSize();
        }
    }
}