
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Linq;
using System.Collections.Generic;

namespace Modules.ExternalAssets
{
    public sealed class LabalPopupView : PopupWindowContent
    {
        //----- params -----

        //----- field -----

		private ManageInfo manageInfo = null;

		private List<string> labeList = null;

		private ReorderableList reorderableList = null;

		//----- property -----

		//----- method -----

		public LabalPopupView(ManageInfo manageInfo)
		{
			this.manageInfo = manageInfo;

			labeList = manageInfo.labels.ToList();
		}

		public override Vector2 GetWindowSize()
		{
			var windowSize = base.GetWindowSize();

			windowSize.x = 185f;

			if (reorderableList != null)
			{
				windowSize.y = reorderableList.GetHeight() + 4f;
			}

			return windowSize;
		}

		public override void OnGUI(Rect rect)
		{
			if (reorderableList == null)
			{
				reorderableList = new ReorderableList(labeList, typeof(string));

				reorderableList.drawHeaderCallback = r => EditorGUI.LabelField(r, "Labels");

				reorderableList.drawElementCallback = (r, index, isActive, isFocused) => 
	            {
					r.position = Vector.SetY(r.position, r.position.y + 2f);
					r.height = EditorGUIUtility.singleLineHeight;
	                
					labeList[index] = EditorGUI.TextField(r, labeList[index]);
	            };
			}

			reorderableList.DoLayoutList();
		}

		public override void OnClose()
		{
			base.OnClose();

			var labels = labeList.Distinct()
				.Where(x => !string.IsNullOrEmpty(x))
				.ToArray();

			manageInfo.labels = labels;
		}
	}
}