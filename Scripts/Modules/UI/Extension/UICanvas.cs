
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Extensions;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    public abstract class UICanvas : UIComponent<Canvas>
    {
        //----- params -----

        //----- field -----
		
        [SerializeField]
        protected bool modifyCanvasCamera = true;
        [SerializeField]
        protected bool modifyCanvasScaler = true;

        protected Camera canvasCamera = null;

        //----- property -----

        public Canvas Canvas { get { return component; } }

		public bool CanvasCameraModify
        {
            get { return modifyCanvasCamera; }
            set { modifyCanvasCamera = value; }
        }

        public bool CanvasScalerModify
        {
            get { return modifyCanvasScaler; }
            set { modifyCanvasScaler = value; }
        }

        protected abstract Vector2 ReferenceResolution { get; }

        //----- method -----

        void OnEnable()
        {
            if (Application.isPlaying)
            {
                Observable.EveryUpdate()
                    .TakeUntilDisable(this)
                    .Subscribe(_ =>
                        {
                            if (modifyCanvasCamera && Canvas.worldCamera == null)
                            {
                                ModifyCanvasCamera();
                            }
                        })
                    .AddTo(this);
            }
            else
            {
                if (modifyCanvasCamera && Canvas.worldCamera == null)
                {
                    ModifyCanvasCamera();
                }
            }
			
            ModifyCanvasCamera();
            ModifyCanvasScaler();
		}

		// Canvasにカメラを適用.
        protected virtual void ModifyCanvasCamera()
        {
            var canvas = Canvas;

            if (!modifyCanvasCamera) { return; }

            var layerMask = 1 << canvas.gameObject.layer;

            // 最初に一致したカメラを適用.
            canvasCamera = UnityUtility.FindCameraForLayer(layerMask).FirstOrDefault();

            if (canvasCamera != null)
            {
                canvas.worldCamera = canvasCamera;
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
            }

            // この関数でカメラを設定した際にNearClipとFarClipの範囲外の場合は警告を出す.
            if (canvas.worldCamera != null)
            {
                var planeDistance = canvas.planeDistance;

                if (planeDistance < canvas.worldCamera.nearClipPlane || canvas.worldCamera.farClipPlane < planeDistance)
                {
                    Debug.LogWarning("Out of Range Canvas PanelDistance.");
                }
            }
        }

        public virtual void ModifyCanvasScaler()
        {
            if (!modifyCanvasScaler) { return; }

            if (!component.isRootCanvas) { return; }

            var canvasScaler = UnityUtility.GetComponent<CanvasScaler>(gameObject);

            if (canvasScaler != null)
            {
                canvasScaler.enabled = false;

                OnModifyCanvasScaler(canvasScaler);

                canvasScaler.enabled = true;
            }
        }

        protected virtual void OnModifyCanvasScaler(CanvasScaler canvasScaler)
        {
            var screenSize = ScreenUtility.GetSize();

            var currentAspectRatio = (float)screenSize.x / screenSize.y;
            var referenceAspectRatio = canvasScaler.referenceResolution.x / canvasScaler.referenceResolution.y;

            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = ReferenceResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

			// デフォルトは横持ち設定.
            canvasScaler.matchWidthOrHeight = currentAspectRatio < referenceAspectRatio ? 0 : 1;
        }
    }
}
