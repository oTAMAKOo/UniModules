
using UnityEngine;
using UnityEngine.EventSystems;

namespace Modules.UI
{
    /// <summary> RectTransformのサイズを指定した基準サイズのアスペクト比に固定 </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class FixedAspectRectTransform : UIBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private Vector2 baseSize = new Vector2(100, 100);

        /// <summary>  trueなら幅基準で高さを合わせる / falseなら高さ基準で幅を合わせる </summary>
        [SerializeField]
        private bool matchWidth = true;

        #if UNITY_EDITOR

        private DrivenRectTransformTracker drivenRectTransformTracker = new DrivenRectTransformTracker();

        #endif

        //----- property -----

        //----- method -----

        protected override void OnEnable()
        {
            base.OnEnable();
            
            UpdateSize();
        }

        #if UNITY_EDITOR

        protected override void OnDisable()
        {
            drivenRectTransformTracker.Clear();
        }

        #endif

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();

            UpdateSize();
        }

        private void UpdateSize()
        {
            var rectTransform = transform as RectTransform;

            if (rectTransform == null || baseSize.y <= 0 || baseSize.x <= 0)
            {
                return;
            }

            var aspect = baseSize.x / baseSize.y;

            var size = rectTransform.sizeDelta;

            // Width基準でHeightを固定.
            if (matchWidth)
            {
                size.y = size.x / aspect;
            }
            // Height基準でWidthを固定.
            else
            {
                size.x = size.y * aspect;
            }

            // 幅を基準に高さを計算
            if (matchWidth)
            {
                var width = rectTransform.rect.width;
                var height = width / aspect;

                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
            // 高さを基準に幅を計算
            else
            {
                var height = rectTransform.rect.height;
                var width = height * aspect;

                rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }

            #if UNITY_EDITOR
            
            drivenRectTransformTracker.Clear();

            var drivenProperties = matchWidth ? DrivenTransformProperties.SizeDeltaY : DrivenTransformProperties.SizeDeltaX;

            drivenRectTransformTracker.Add(this, rectTransform, drivenProperties);

            #endif
        }

        #if UNITY_EDITOR

        protected override void OnValidate()
        {
            base.OnValidate();

            UpdateSize();
        }

        #endif
    }
}