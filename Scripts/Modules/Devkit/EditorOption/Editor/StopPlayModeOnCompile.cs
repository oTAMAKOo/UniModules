
#if !UNITY_2017_1_OR_NEWER

using UnityEngine;
using UnityEditor;
using System;
using Modules.Devkit.CompileNotice;
using Modules.Devkit.Prefs;
using UniRx;

namespace Modules.Devkit
{
    public static class StopPlayModeOnCompilePrefs
    {
        public static bool enable
        {
            get { return ProjectPrefs.GetBool("StopPlayModeOnCompilePrefs-enable", false); }
            set { ProjectPrefs.SetBool("StopPlayModeOnCompilePrefs-enable", value); }
        }
    }

    public static class StopPlayModeOnCompile
	{
        //----- params -----

        //----- field -----

        private static IDisposable disposable = null;

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            SetEnable(StopPlayModeOnCompilePrefs.enable);
        }

        public static void SetEnable(bool state)
        {
            StopPlayModeOnCompilePrefs.enable = state;

            if (disposable != null)
            {
                disposable.Dispose();
            }

            if (state)
            {
                disposable = CompileNotification.OnCompileStartAsObservable().Subscribe(
                    _ =>
                    {
                        if (EditorApplication.isPlaying)
                        {
                            EditorApplication.isPlaying = false;
                        }
                    });
            }
        }
    }
}

#endif
