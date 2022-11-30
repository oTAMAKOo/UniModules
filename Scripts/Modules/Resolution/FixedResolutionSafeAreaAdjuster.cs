
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using System.Linq;
using Extensions;

namespace Modules.Resolution
{
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public sealed class FixedResolutionSafeAreaAdjuster : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private Canvas canvas = null;
		private CanvasScaler canvasScaler = null;

        private Rect lastSafeArea = default;
        private Vector2 lastResolution = default;

        #if UNITY_EDITOR

        private DrivenRectTransformTracker drivenRectTransformTracker = new DrivenRectTransformTracker();

        #endif

        //----- property -----

        //----- method -----

        void Update()
        {
            Apply();
        }

        void OnEnable()
        {
            canvas = gameObject.Ancestors().OfComponent<Canvas>().FirstOrDefault(x => x.isRootCanvas);
			canvasScaler = UnityUtility.GetComponent<CanvasScaler>(canvas);

            Apply();
        }

        #if UNITY_EDITOR

        void OnDisable()
        {
            drivenRectTransformTracker.Clear();
        }

        #endif

        public void Apply(bool force = false)
        {
			var rt = transform as RectTransform;

            if (rt == null){ return; }

            var safeArea = Screen.safeArea;

            var canvasRt = canvas.transform as RectTransform;

            var resolution = canvasRt.GetSize();

            if (resolution.x == 0 || resolution.y == 0) { return; }

            #if UNITY_EDITOR

            var drivenProperties = DrivenTransformProperties.AnchoredPosition |
                                   DrivenTransformProperties.SizeDelta |
                                   DrivenTransformProperties.AnchorMin | 
                                   DrivenTransformProperties.AnchorMax;

            drivenRectTransformTracker.Clear();
            drivenRectTransformTracker.Add(this, rt,drivenProperties);

            #endif

            if (!force)
            {
                // ※Undoすると0になるので再適用.
                if (rt.anchorMax != Vector2.zero)
                {
                    // 差分がなければスキップ.
                    if (lastSafeArea == safeArea && lastResolution == resolution)
                    {
                        return;
                    }
                }
            }

            lastSafeArea = safeArea;
            lastResolution = resolution;

            var scaledSafeArea = new Rect
            {
                xMin = safeArea.xMin / canvas.scaleFactor,
                xMax = safeArea.xMax / canvas.scaleFactor,
                yMin = safeArea.yMin / canvas.scaleFactor,
                yMax = safeArea.yMax / canvas.scaleFactor,
            };

            var baseOffsetX = (resolution.x - canvasScaler.referenceResolution.x) * 0.5f;
            var baseOffsetY = (resolution.y - canvasScaler.referenceResolution.y) * 0.5f;

            var top = resolution.y - baseOffsetY;

            if (scaledSafeArea.yMax < top)
            {
                top = scaledSafeArea.yMax;
            }

            var bottom = baseOffsetY;

            if (bottom < scaledSafeArea.yMin)
            {
                bottom = scaledSafeArea.yMin;
            }

            var left = baseOffsetX;

            if (left < scaledSafeArea.xMin)
            {
                left = scaledSafeArea.xMin;
            }

            var right = resolution.x - baseOffsetX;

            if (scaledSafeArea.xMax< right)
            {
                right = scaledSafeArea.xMax;
            }
            
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;

            rt.anchorMin = new Vector2(left / resolution.x, bottom / resolution.y);
            rt.anchorMax = new Vector2(right / resolution.x, top / resolution.y);

            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            Canvas.ForceUpdateCanvases();
        }
    }
}