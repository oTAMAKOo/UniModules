using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using Extensions.Devkit;
using Modules.Devkit.Prefs;
using Modules.TextData.Editor;

namespace Modules.Localize
{
	public sealed class LanguageSelector : SingletonEditorWindow<LanguageSelector>
	{
		//----- params -----

		public const string WindowTitle = "Language";

		private sealed class Prefs
		{
			public static string assembly
			{
				get { return ProjectPrefs.GetString(typeof(Prefs).FullName + "-assembly", null); }
				set { ProjectPrefs.SetString(typeof(Prefs).FullName + "-assembly", value); }
			}

			public static string enumType
			{
				get { return ProjectPrefs.GetString(typeof(Prefs).FullName + "-enumType", null); }
				set { ProjectPrefs.SetString(typeof(Prefs).FullName + "-enumType", value); }
			}
		}

		//----- field -----

		private Type enumType = null;

		//----- property -----

		//----- method -----

		public static void Open(Type enumType)
		{
			if (enumType != null)
			{
				Prefs.assembly = enumType.Assembly.FullName;
				Prefs.enumType = enumType.FullName;
			}

			Instance.Initialize();
		}

		private void Initialize()
		{
			titleContent = new GUIContent(WindowTitle);

			minSize = new Vector2(100, 35f);

			if (string.IsNullOrEmpty(Prefs.assembly) || string.IsNullOrEmpty(Prefs.enumType))
			{
				throw new InvalidDataException();
			}
            
			var assembly = Assembly.Load(Prefs.assembly);

			enumType = assembly.GetType(Prefs.enumType);

			Show(true);
		}

		void OnGUI()
		{
			Initialize();

			GUILayout.Space(8f);

			EditorGUI.BeginChangeCheck();

			var names = System.Enum.GetNames(enumType);

			var selection = EditorGUILayout.Popup(EditorLanguage.selection, names);

			if (EditorGUI.EndChangeCheck())
			{
				EditorLanguage.selection = selection;

				TextDataLoader.Reload();
			}

			GUILayout.Space(8f);
		}
	}
}
