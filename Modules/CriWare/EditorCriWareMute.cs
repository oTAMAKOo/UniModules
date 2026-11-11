#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE

#if UNITY_EDITOR

using Modules.Devkit.Prefs;

namespace Modules.CriWare
{
    public static class EditorCriWareMute
    {
        //----- params -----

        public static class Prefs
        {
            public static bool editorAudioMute
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-editorAudioMute", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-editorAudioMute", value); }
            }
        }
        
        //----- field -----

        //----- property -----

        //----- method -----
    }
}

#endif

#endif