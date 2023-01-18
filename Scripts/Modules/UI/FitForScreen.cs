
using UnityEngine;
using Unity.Linq;
using System.Linq;

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

		//----- property -----

        //----- method -----

		void OnEnable()
		{
			var canvas = gameObject.Ancestors().OfComponent<Canvas>().FirstOrDefault(x => x.isRootCanvas);

			canvasRectTransform = canvas != null ? canvas.transform as RectTransform : null;

			rectTransform = transform as RectTransform;

			UpdateTransform();
		}

		void Update()
		{
			UpdateTransform();
		}

		private void UpdateTransform()
		{
			if (rectTransform == null){ return; }

			if (canvasRectTransform == null){ return; }

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

			rectTransform.localPosition = Vector3.zero;
			rectTransform.localRotation = Quaternion.identity;
			rectTransform.localScale = localScale;

			rectTransform.pivot = new Vector2 (0.5f, 0.5f);
			rectTransform.sizeDelta = canvasRectTransform.sizeDelta;
		}
	}
}