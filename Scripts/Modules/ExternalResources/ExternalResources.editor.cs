﻿﻿
#if UNITY_EDITOR

using Modules.Devkit.Prefs;

namespace Modules.ExternalResource
{
	public sealed partial class ExternalResources
	{
        public static class Prefs
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
