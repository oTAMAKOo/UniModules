
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

			if (string.IsNullOrEmpty(instance.FocusId))
			{
                instance.GenerateFocusId();

				EditorUtility.SetDirty(instance);
			}
			
			using (new EditorGUILayout.HorizontalScope())
			{
				using (new LabelWidthScope(80f))
				{
					EditorGUILayout.PrefixLabel("FocusId");
				}

				GUILayout.FlexibleSpace();

				var size = GUI.skin.label.CalcSize(new GUIContent(instance.FocusId));

				var layoutOptions = new GUILayoutOption[]
				{
					GUILayout.Width(size.x + 15f),
					GUILayout.Height(EditorGUIUtility.singleLineHeight),
				};

				EditorGUILayout.SelectableLabel(instance.FocusId, EditorStyles.textArea, layoutOptions);

				if (GUILayout.Button(clipboardIcon, GUILayout.Width(35f)))
				{
					GUIUtility.systemCopyBuffer = instance.FocusId;
				}
			}
		}
    }
}