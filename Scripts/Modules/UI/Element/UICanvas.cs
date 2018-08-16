
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;


namespace Modules.UI.Element
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Canvas))]
    public abstract class UICanvas : UIElement<Canvas>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        protected RectTransform canvasRoot = null;
        [SerializeField]
        protected bool modifyCanvasCamera = true;
        [SerializeField]
        protected bool modifyCanvasScaler = true;

        protected Camera canvasCamera = null;

        //----- property -----

        public Canvas Canvas { get { return component; } }

        public RectTransform CanvasRoot
        {
            get { return canvasRoot; }
            set { canvasRoot = value; }
        }

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

        protected abstract Camera[] Cameras { get; }
        protected abstract CanvasScaler.ScaleMode ScaleMode { get; }
        protected abstract Vector2 ReferenceResolution { get; }

        //----- method -----

        protected override void OnEnable()
        {
            base.OnEnable();

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
        }

        public override void Modify()
        {
            ModifyCanvasRoot();
            ModifyCanvasCamera();
            ModifyCanvasScaler();
        }

        protected virtual void ModifyCanvasRoot()
        {
            if (canvasRoot == null) { return; }

            if (!component.isRootCanvas) { return; }

            canvasRoot.Reset();

            canvasRoot.anchorMin = new Vector2(0.5f, 0.5f);
            canvasRoot.anchorMax = new Vector2(0.5f, 0.5f);

            canvasRoot.SetSize(ReferenceResolution);
        }

        // Canvasにカメラを適用.
        protected virtual void ModifyCanvasCamera()
        {
            var canvas = Canvas;

            if (!modifyCanvasCamera) { return; }

            var layer = 1 << canvas.gameObject.layer;

            // 最初に一致したカメラを適用.
            foreach (var camera in Cameras)
            {
                if (UnityUtility.IsNull(camera)) { continue; }

                if ((layer & camera.cullingMask) != 0)
                {
                    canvasCamera = camera;
                    break;
                }
            }

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
                canvasScaler.uiScaleMode = ScaleMode;

                if (canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
                {
                    canvasScaler.referenceResolution = ReferenceResolution;
                    canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

                    var currentAspectRatio = (float)Screen.width / Screen.height;
                    var referenceAspectRatio = canvasScaler.referenceResolution.x / canvasScaler.referenceResolution.y;

                    canvasScaler.matchWidthOrHeight = currentAspectRatio < referenceAspectRatio ? 0 : 1;
                }
            }
        }
    }
}