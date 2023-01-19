
using UnityEngine;
using Unity.Linq;
using System.Linq;
using Extensions;

namespace Modules.UI
{
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
    public sealed class FitForScreen : MonoBehaviour
    {
        //----- params -----

        //----- field -----

		private RectTransform canvasRectTransform = null;

		private RectTransform rectTransform = null;

		#if UNITY_EDITOR

		private DrivenRectTransformTracker drivenRectTransformTracker = new DrivenRectTransformTracker();

		#endif

		//----- property -----

        //----- method -----

		void OnEnable()
		{
			var canvas = gameObject.Ancestors().OfComponent<Canvas>().FirstOrDefault(x => x.isRootCanvas);

			canvasRectTransform = canvas != null ? canvas.transform as RectTransform : null;

			rectTransform = transform as RectTransform;

			Apply();
		}

		#if UNITY_EDITOR

		void OnDisable()
		{
			drivenRectTransformTracker.Clear();
		}

		#endif

		void Update()
		{
			Apply();
		}

		public void Apply()
		{
			if (rectTransform == null){ return; }

			if (canvasRectTransform == null){ return; }

			#if UNITY_EDITOR

			var drivenProperties = DrivenTransformProperties.All;

			drivenRectTransformTracker.Clear();
			drivenRectTransformTracker.Add(this, rectTransform,drivenProperties);

			#endif

			var parent = rectTransform.parent;

			var lossyScale = parent.lossyScale;

			var localScale = new Vector3(
				lossyScale.x != 0f ? 1f / lossyScale.x : 1f,
				lossyScale.y != 0f ? 1f / lossyScale.y : 1f,
				lossyScale.z != 0f ? 1f / lossyScale.z : 1f
			);

			localScale.x *= canvasRectTransform.localScale.x;
			localScale.y *= canvasRectTransform.localScale.y;
			localScale.z *= canvasRectTransform.localScale.z;

			rectTransform.position = Vector3.zero;
			rectTransform.localPosition = Vector.SetZ(rectTransform.localPosition, 0f);
			rectTransform.localRotation = Quaternion.identity;
			rectTransform.localScale = localScale;
			
			rectTransform.sizeDelta = Vector2.zero;
			rectTransform.pivot = new Vector2 (0.5f, 0.5f);
			rectTransform.SetSize(canvasRectTransform.sizeDelta);
		}
	}
}