﻿
#if UNITY_EDITOR

using Modules.Devkit.Prefs;

namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset
    {
        public static partial class Prefs
        {
            public static bool isSimulate
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-isSimulate", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-isSimulate", value); }
            }
        }
    }
}

#endif
