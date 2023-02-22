
using UnityEngine;
using UnityEditor;
using System;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.Focus
{
	[CustomEditor(typeof(FocusTarget))]
    public sealed class FocusTargetInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

		private static GUIContent clipboardIcon = null;

        //----- property -----

        //----- method -----

		void OnEnable()
		{
			if (clipboardIcon == null)
			{
				clipboardIcon = EditorGUIUtility.IconContent("Clipboard");
			}
		}

		public override void OnInspectorGUI()
		{
			var instance = target as FocusTarget;

			GUILayout.Space(4f);

			DrawDefaultScriptlessInspector();

			var focusId = Reflection.GetPrivateField<FocusTarget, string>(instance, "focusId");

			if (string.IsNullOrEmpty(focusId))
			{
				focusId = Guid.NewGuid().ToString("N");

				Reflection.SetPrivateField(instance, "focusId", focusId);

				EditorUtility.SetDirty(instance);
			}
			
			using (new EditorGUILayout.HorizontalScope())
			{
				using (new LabelWidthScope(80f))
				{
					EditorGUILayout.PrefixLabel("FocusId");
				}

				GUILayout.FlexibleSpace();

				var size = GUI.skin.label.CalcSize(new GUIContent(focusId));

				var layoutOptions = new GUILayoutOption[]
				{
					GUILayout.Width(size.x + 15f),
					GUILayout.Height(EditorGUIUtility.singleLineHeight),
				};

				EditorGUILayout.SelectableLabel(focusId, EditorStyles.textArea, layoutOptions);

				if (GUILayout.Button(clipboardIcon, GUILayout.Width(35f)))
				{
					GUIUtility.systemCopyBuffer = focusId;
				}
			}
		}
    }
}