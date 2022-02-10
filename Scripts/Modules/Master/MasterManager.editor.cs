
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
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-checkVersion", true); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-checkVersion", value); }
            }
        }
    }
}

#endif
