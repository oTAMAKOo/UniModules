
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using System;
using Cysharp.Threading.Tasks;
using UniRx;
using TMPro;
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

        public string FocusId
        {
            get
            {
                if (string.IsNullOrEmpty(focusId))
                {
                    focusId = Guid.NewGuid().ToString("N");
                }

                return focusId;
            }
        }

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


        void OnDestroy()
        {
            var focusManager = FocusManager.Instance;

            focusManager.Leave(this);
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
            var focusManager = FocusManager.Instance;

			if (IsFocus){ return; }

            Setup();

			IsFocus = true;

            originOverrideSorting = canvasSelf.overrideSorting;
            originSortingOrder = canvasSelf.sortingOrder;

            ApplyCanvasSelf(true, canvas.sortingOrder + 1).Forget();

            focusManager.Join(this);
		}

        public void Release()
        {
            var focusManager = FocusManager.Instance;

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

            focusManager.Leave(this);
        }

		private void UpdateFocus()
		{
			var focusManager = FocusManager.Instance;

			if (string.IsNullOrEmpty(FocusId)){ return; }

			var isTarget = focusManager.Contains(FocusId);

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

            var textComponents = gameObject.DescendantsAndSelf().OfComponent<TextMeshProUGUI>();

            foreach (var textComponent in textComponents)
            {
                textComponent.SetAllDirty();

                textComponent.ForceMeshUpdate(true);
            }
        }

        private async UniTask ApplyCanvasSelf(bool overrideSorting, int sortingOrder)
        {
            try
            {
                if (UnityUtility.IsNull(gameObject)){ return; }

                var cancelToken = gameObject.GetCancellationTokenOnDestroy();

                while (!cancelToken.IsCancellationRequested)
                {
                    if (UnityUtility.IsActiveInHierarchy(gameObject)){ break; }

                    await UniTask.NextFrame(cancellationToken: cancelToken);
                }

                if (UnityUtility.IsNull(canvasSelf)){ return; }
            
                canvasSelf.overrideSorting = overrideSorting;
                canvasSelf.sortingOrder = sortingOrder;
            }
            catch (OperationCanceledException)
            {
                /* Canceled */
            }
        }
    }
}