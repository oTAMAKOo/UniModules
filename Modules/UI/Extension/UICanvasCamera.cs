
using UnityEngine;
using System.Linq;
using Extensions;

namespace Modules.UI.Extension
{
	/// <summary> UICanvasのCamera自動設定の対象カメラ </summary>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Camera))]
    public sealed class UICanvasCamera : MonoBehaviour
    {
		//----- params -----

		//----- field -----

		//----- property -----

		//----- method -----

		/// <summary>
		/// UICanvas用カメラ取得.
		/// ※ layerMaskは <code>1 &lt;&lt; (int)layer</code>された状態の物を受け取る.
		/// </summary>
		public static Camera GetCanvasCameraForLayer(int layerMask)
		{
			return UnityUtility.FindCameraForLayer(layerMask).FirstOrDefault(x => x.GetComponent<UICanvasCamera>() != null);
		}
	}
}