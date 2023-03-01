
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using UnityEngine.UI;

namespace Modules.Resolution
{
	[ExecuteAlways]
	[RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaAdjuster : MonoBehaviour
    {
        //----- params -----

        //----- field -----

		private Rect lastSafeArea = default;
		private Vector2Int lastResolution = default;

		#if UNITY_EDITOR

		private DrivenRectTransformTracker drivenRectTransformTracker = new DrivenRectTransformTracker();

		#endif

		//----- property -----

        //----- method -----

		void Update()
		{
			Apply();
		}

		void OnEnable()
		{
			Apply();
		}

		#if UNITY_EDITOR

		void OnDisable()
		{
			drivenRectTransformTracker.Clear();
		}

		#endif

		public void Apply(bool force = false)
		{
			var rt = transform as RectTransform;

			if (rt == null){ return; }

			var safeArea = Screen.safeArea;
			var resolution = new Vector2Int(Screen.width, Screen.height);

			if (resolution.x == 0 || resolution.y == 0)
			{
				return;
			}

			#if UNITY_EDITOR

			var drivenProperties = DrivenTransformProperties.All;

			drivenRectTransformTracker.Clear();
			drivenRectTransformTracker.Add(this, rt,drivenProperties);

			#endif

			if (!force)
			{
				// ※Undoすると0になるので再適用.
				if (rt.anchorMax != Vector2.zero)
				{
					// 差分がなければスキップ.
					if (lastSafeArea == safeArea && lastResolution == resolution)
					{
						return;
					}
				}
			}

			lastSafeArea = safeArea;
			lastResolution = resolution;

			transform.Reset();

			var normalizedMin = new Vector2(safeArea.xMin / resolution.x, safeArea.yMin / resolution.y);
			var normalizedMax = new Vector2(safeArea.xMax / resolution.x, safeArea.yMax / resolution.y);

			rt.anchoredPosition = Vector2.zero;
			rt.sizeDelta = Vector2.zero;
			rt.anchorMin = normalizedMin;
			rt.anchorMax = normalizedMax;
		}
	}
}