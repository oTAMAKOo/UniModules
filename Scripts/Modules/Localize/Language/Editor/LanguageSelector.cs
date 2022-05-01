
using UnityEngine;
using UnityEditor;
using System;
using Extensions.Devkit;
using Modules.TextData.Editor;

namespace Modules.Localize.Editor
{
    public sealed class LanguageSelector : SingletonEditorWindow<LanguageSelector>
    {
        //----- params -----

		public const string WindowTitle = "LanguageSelector";

        //----- field -----

		private Type enumType = null;

        //----- property -----

        //----- method -----

		public static void Open(Type enumType)
		{
			Instance.Initialize(enumType);
		}

		private void Initialize(Type enumType)
		{
			this.enumType = enumType;

			titleContent = new GUIContent(WindowTitle);

			minSize = new Vector2(100, 35f);

			Show(true);
		}

		void OnGUI()
		{
			GUILayout.Space(8f);

			EditorGUI.BeginChangeCheck();

			var names = System.Enum.GetNames(enumType);

			var selection = EditorGUILayout.Popup(Language.selection, names);

			if (EditorGUI.EndChangeCheck())
			{
				Language.selection = selection;

				TextDataLoader.Reload();
			}

			GUILayout.Space(8f);
		}
    }
}