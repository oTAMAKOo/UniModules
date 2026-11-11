
using System.Linq;
using UnityEditor;
using Extensions;
using Extensions.Devkit;
using UnityEngine;

namespace Modules.UI.DummyContent
{
	[CustomEditor(typeof(DummyText))]
	public sealed class DummyTextInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

		private DummyText instance = null;

        //----- property -----

        //----- method -----

		public override void OnInspectorGUI()
		{
			instance = target as DummyText;

			DrawDummyTextSelectGUI();
		}

		private void DrawDummyTextSelectGUI()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				var labelWidth = EditorGUIUtility.labelWidth - 10f;

				EditorGUILayout.LabelField("DummyText", GUILayout.Width(labelWidth));

				var editText = string.Empty;

				var dummyText = (string)Reflection.InvokePrivateMethod(instance, "GetDummyText");

				if (!string.IsNullOrEmpty(dummyText))
				{
					editText = dummyText.TrimStart(DummyText.DummyMark);
				}

				var prevText = editText;

				EditorGUI.BeginChangeCheck();

				var lineCount = editText.Count(x => x == '\n') + 1;

				lineCount = Mathf.Clamp(lineCount, 1, 5);

				var textAreaHeight = lineCount * 18f;

				editText = EditorGUILayout.TextArea(editText, GUILayout.ExpandWidth(true), GUILayout.Height(textAreaHeight));

				if (EditorGUI.EndChangeCheck())
				{
					UnityEditorUtility.RegisterUndo(instance);

					if (!string.IsNullOrEmpty(prevText))
					{
						Reflection.InvokePrivateMethod(instance, "ApplyText", new object[] { null });
					}

					if (!string.IsNullOrEmpty(editText))
					{
						editText = editText.FixLineEnd();
					}

					SetDummyText(editText);
				}
			}
		}

		private void SetDummyText(string text)
		{
			Reflection.InvokePrivateMethod(instance, "SetDummyText", new object[] { text });
		}
	}
}