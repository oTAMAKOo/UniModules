﻿﻿
#if UNITY_EDITOR

using Modules.Devkit.Prefs;

namespace Modules.ExternalResource
{
	public partial class ExternalResources
	{
        public static class Prefs
        {
            public static bool isSimulate
            {
                get { return ProjectPrefs.GetBool("ExternalResources-Prefs-isSimulate", false); }
                set { ProjectPrefs.SetBool("ExternalResources-Prefs-isSimulate", value); }
            }
        }
    }
}

#endif