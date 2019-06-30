
#if UNITY_EDITOR

using Modules.Devkit.Prefs;

namespace Modules.Master
{
    public sealed partial class MasterManager
    {
        public static class Prefs
        {
            public static bool checkVersion
            {
                get { return ProjectPrefs.GetBool("MasterManager-Prefs-checkVersion", true); }
                set { ProjectPrefs.SetBool("MasterManager-Prefs-checkVersion", value); }
            }
        }
    }
}

#endif
