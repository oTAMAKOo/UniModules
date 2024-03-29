﻿
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.UI.Focus
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class FocusTarget : MonoBehaviour
    {
        //----- params -----

        //----- field -----

		[SerializeField, HideInInspector]
		private string focusId = null;

		private Canvas canvasSelf = null;

		private GraphicRaycaster raycasterSelf = null;

        private bool addCanvas = false;

        private bool addRaycaster = false;
        
        private bool originOverrideSorting = false;

        private int originSortingOrder = 0;

        //----- property -----

		public bool IsFocus { get; private set; }

		//----- method -----

		void Awake()
		{
			var focusManager = FocusManager.Instance;

			focusManager.OnUpdateFocusAsObservable()
				.Subscribe(_ => UpdateFocus())
				.AddTo(this);
		}
		
		void OnEnable()
		{
			UpdateFocus();
		}

        private void Setup()
        {
			canvasSelf = UnityUtility.GetComponent<Canvas>(gameObject);

            addCanvas = canvasSelf == null;

            if (addCanvas)
            {
				canvasSelf = UnityUtility.AddComponent<Canvas>(gameObject);
            }

			raycasterSelf = UnityUtility.GetComponent<GraphicRaycaster>(gameObject);

            addRaycaster = raycasterSelf == null;

            if (addRaycaster)
            {
				raycasterSelf = UnityUtility.AddComponent<GraphicRaycaster>(gameObject);
            }
        }

        public void Focus(Canvas canvas)
        {
			if (IsFocus){ return; }

            Setup();

			IsFocus = true;

            originOverrideSorting = canvasSelf.overrideSorting;
            originSortingOrder = canvasSelf.sortingOrder;

            ApplyCanvasSelf(true, canvas.sortingOrder + 1).Forget();
		}

        public void Release()
        {
			if (!IsFocus){ return; }

			IsFocus = false;

            if (addRaycaster)
            {
                UnityUtility.DeleteComponent(raycasterSelf);
				raycasterSelf = null;
            }

            if (addCanvas)
            {
                UnityUtility.DeleteComponent(canvasSelf);
				canvasSelf = null;
            }
            else
            {
                ApplyCanvasSelf(originOverrideSorting, originSortingOrder).Forget();
            }
        }

		private void UpdateFocus()
		{
			var focusManager = FocusManager.Instance;

			if (string.IsNullOrEmpty(focusId)){ return; }

			var isTarget = focusManager.Contains(focusId);

			if (isTarget)
			{
				if (focusManager.FocusCanvas != null)
				{
					Focus(focusManager.FocusCanvas);
				}
			}
			else
			{
				Release();
			}
		}

        private async UniTask ApplyCanvasSelf(bool overrideSorting, int sortingOrder)
        {
            while (!UnityUtility.IsActiveInHierarchy(gameObject))
            {
                await UniTask.NextFrame();
            }

            canvasSelf.overrideSorting = overrideSorting;
            canvasSelf.sortingOrder = sortingOrder;
        }
    }
}