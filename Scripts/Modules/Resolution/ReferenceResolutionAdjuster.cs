
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Unity.Linq;
using UniRx;
using Extensions;

namespace Modules.Resolution
{
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
    public sealed class ReferenceResolutionAdjuster : MonoBehaviour
    {
		//----- params -----

		//----- field -----

		//----- property -----

		//----- method -----

		void OnEnable()
		{
			if (Application.isPlaying)
			{
				// OnEnableでApplyしてもRectTransformに反映されない事があるので最初のUpdateで再度実行.
				Observable.EveryUpdate()
					.First()
					.TakeUntilDisable(this)
					.Subscribe(_ => Apply())
					.AddTo(this);
			}

			Apply();
		}

		private void Apply()
		{
			var rt = transform as RectTransform;

			if (rt == null) { return; }

			var referenceResolution = GetReferenceResolution();

			if (referenceResolution.HasValue)
			{
				rt.Reset();
				rt.anchorMin = new Vector2(0.5f, 0.5f);
				rt.anchorMax = new Vector2(0.5f, 0.5f);
				rt.SetSize(referenceResolution.Value);
			}
		}

		private Vector2? GetReferenceResolution()
		{
			var rootCanvas = gameObject.Ancestors().OfComponent<Canvas>().FirstOrDefault(x => x.isRootCanvas);

			if (rootCanvas == null){ return null; }

			var canvasScaler = UnityUtility.GetComponent<CanvasScaler>(rootCanvas);

			if (canvasScaler == null){ return null; }

			return canvasScaler.referenceResolution;
		}
    }
}