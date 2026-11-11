
#if UNITY_EDITOR

using Modules.Devkit.Prefs;

namespace Modules.Localize
{
	public static class EditorLanguage
	{
		public static int selection
		{
			get { return ProjectPrefs.GetInt(typeof(EditorLanguage).FullName + "-selection", -1); }
			set { ProjectPrefs.SetInt(typeof(EditorLanguage).FullName + "-selection", value); }
		}
	}
}

#endif