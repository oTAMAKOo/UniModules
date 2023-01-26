
using UnityEngine;
using UnityEngine.UI;
using Extensions;

namespace Modules.UI.Focus
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class FocusTarget : MonoBehaviour
    {
        //----- params -----

        //----- field -----

		private Canvas canvasSelf = null;

		private GraphicRaycaster raycasterSelf = null;

        private bool addCanvas = false;

        private bool addRaycaster = false;
        
        private bool originOverrideSorting = false;

        private int originSortingOrder = 0;

        //----- property -----

		public bool IsFocus { get; private set; }

		//----- method -----

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

			canvasSelf.overrideSorting = true;
			canvasSelf.sortingOrder = canvas.sortingOrder + 1;
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
				canvasSelf.overrideSorting = originOverrideSorting;
				canvasSelf.sortingOrder = originSortingOrder;
            }
        }
    }
}