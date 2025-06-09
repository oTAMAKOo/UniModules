
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.OffScreenRendering
{
    [RequireComponent(typeof(RawImage))]
    public abstract class RenderTextureRaycaster : MonoBehaviour, IPointerClickHandler
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private RenderTarget renderTarget = null;

        private RawImage rawImage = null;
        private RectTransform rectTransform = null;
        private Camera currenCamera = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public void Initialize()
        {
            if (initialized) { return; }

            rawImage = UnityUtility.GetComponent<RawImage>(gameObject);

            rectTransform = rawImage.rectTransform;

            // レンダーテクスチャ描画用カメラ以外のカメラから検索.
            currenCamera = UnityUtility.FindCameraForLayer(1 << gameObject.layer)
                .FirstOrDefault(x => UnityUtility.GetComponent<RenderTarget>(x.gameObject) == null);

            // RawImageのワールド座標サイズ.
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            // レンダーテクスチャのサイズを計算.
            var bl = currenCamera.WorldToScreenPoint(corners[0]);
            var tr = currenCamera.WorldToScreenPoint(corners[2]);

            var width = tr.x - bl.x;
            var height = tr.y - bl.y;

            // レンダーテクスチャ生成.
            rawImage.texture = renderTarget.CreateRenderTexture((int)width, (int)height);

            initialized = true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!initialized) { return; }

            if (currenCamera == null) { return; }

            var screenPosition = eventData.pointerCurrentRaycast.screenPosition;

            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            // RawImageの左下スクリーン座標.
            var screenCorner = currenCamera.WorldToScreenPoint(corners[0]);

            var textureClick = new Vector3()
            {
                x = screenPosition.x - screenCorner.x,
                y = screenPosition.y - screenCorner.y,
                z = 0f,
            };

            var ray = renderTarget.RenderCamera.ScreenPointToRay(textureClick);

            Raycast(ray);
        }

        protected abstract void Raycast(Ray ray);
    }
}