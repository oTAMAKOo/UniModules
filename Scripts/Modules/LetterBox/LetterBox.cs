
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using UniRx;
using Extensions;
using Modules.UI.Extension;

namespace Modules.LetterBox
{
    [ExecuteAlways]
    public sealed class LetterBox : SingletonMonoBehaviour<LetterBox>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private UICanvas uiCanvas = null;
        [SerializeField]
        private RectTransform statusBar = null;
        [SerializeField]
        private RectTransform top = null;
        [SerializeField]
        private RectTransform bottom = null;
        [SerializeField]
        private RectTransform left = null;
        [SerializeField]
        private RectTransform right = null;

        //----- property -----

        public float StatusBarHeight { get; set; }

        //----- method -----

        void OnEnable()
        {
            Apply();
        }

        public void Apply()
        {
            var canvas = uiCanvas.Canvas;
            var canvasScaler = UnityUtility.GetComponent<CanvasScaler>(uiCanvas);

            var enable = canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize;

            UnityUtility.SetActive(gameObject, enable);

            if (!enable) { return; }

            uiCanvas.ModifyCanvasScaler();
            
            var canvasRectTransform = UnityUtility.GetComponent<RectTransform>(canvas.gameObject);

            LayoutRebuilder.ForceRebuildLayoutImmediate(canvasRectTransform);
            
            var letterBoxSize = new Vector2()
            {
                x = (canvasRectTransform.GetWidth() - canvasScaler.referenceResolution.x) * 0.5f,
                y = (canvasRectTransform.GetHeight() - canvasScaler.referenceResolution.y) * 0.5f,
            };

            var fitVertical = canvasScaler.matchWidthOrHeight == 1;

            if (statusBar != null)
            {
                UnityUtility.SetActive(statusBar.gameObject, 0 < StatusBarHeight);
                statusBar.sizeDelta = Vector.SetY(statusBar.sizeDelta, StatusBarHeight);
            }

            if (top != null)
            {
                UnityUtility.SetActive(top.gameObject, !fitVertical);

                if (!fitVertical)
                {
                    top.anchorMin = new Vector2(0f, 1f);
                    top.anchorMax = new Vector2(1f, 1f);
                    top.anchoredPosition = Vector2.zero;
                    top.offsetMax = Vector.SetY(top.offsetMax, 0);
                    top.sizeDelta = new Vector2(0f, letterBoxSize.y + StatusBarHeight);
                }
            }

            if (bottom != null)
            {
                UnityUtility.SetActive(bottom.gameObject, !fitVertical);

                if (!fitVertical)
                {
                    bottom.anchorMin = new Vector2(0f, 0f);
                    bottom.anchorMax = new Vector2(1f, 0f);
                    bottom.anchoredPosition = Vector2.zero;
                    bottom.offsetMax = Vector.SetY(bottom.offsetMax, 0);
                    bottom.sizeDelta = new Vector2(0f, letterBoxSize.y);
                }
            }

            if (left != null)
            {
                UnityUtility.SetActive(left.gameObject, fitVertical);

                if (fitVertical)
                {
                    left.anchorMin = new Vector2(0f, 0f);
                    left.anchorMax = new Vector2(0f, 1f);
                    left.anchoredPosition = Vector2.zero;
                    left.offsetMax = Vector.SetY(left.offsetMax, 0);
                    left.sizeDelta = new Vector2(letterBoxSize.x, 0f);
                }
            }

            if (right != null)
            {
                UnityUtility.SetActive(right.gameObject, fitVertical);

                if (fitVertical)
                {
                    right.anchorMin = new Vector2(1f, 0f);
                    right.anchorMax = new Vector2(1f, 1f);
                    right.anchoredPosition = Vector2.zero;
                    right.offsetMax = Vector.SetY(right.offsetMax, 0);
                    right.sizeDelta = new Vector2(letterBoxSize.x, 0f);
                }
            }
        }
    }
}
