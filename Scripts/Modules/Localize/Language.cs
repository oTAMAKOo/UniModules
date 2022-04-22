
using Modules.Devkit.Prefs;

namespace Modules.Localize
{
	public static class Language
	{
		public static int selection
		{
			get { return ProjectPrefs.GetInt(typeof(Language).FullName + "-selection", -1); }
			set { ProjectPrefs.SetInt(typeof(Language).FullName + "-selection", value); }
		}
	}
}