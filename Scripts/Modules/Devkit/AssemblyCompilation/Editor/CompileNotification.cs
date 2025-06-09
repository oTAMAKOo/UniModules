
using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UniRx;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.AssemblyCompilation
{
    public static class CompileNotification
    {
        //----- params -----

        public static class Prefs
        {
            public static bool isCompiling
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-isCompiling", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-isCompiling", value); }
            }

            public static string compileStartTime
            {
                get { return ProjectPrefs.GetString(typeof(Prefs).FullName + "-compileStartTime", string.Empty); }
                set { ProjectPrefs.SetString(typeof(Prefs).FullName + "-compileStartTime", value); }
            }
        }

        //----- field -----

        private static Subject<Unit> onCompileStart = null;
        private static Subject<Unit> onCompileFinish = null;

        //----- property -----

        
        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            Prefs.isCompiling = false;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (EditorApplication.isCompiling)
            {
                //------ コンパイル開始 ------

                if (!Prefs.isCompiling)
                {
                    Prefs.isCompiling = true;

                    if (onCompileStart != null)
                    {
                        onCompileStart.OnNext(Unit.Default);
                    }
                }
            }
            else
            {
                //------ コンパイル終了 ------

                if(Prefs.isCompiling)
                {
                    Prefs.isCompiling = false;

                    if (onCompileFinish != null)
                    {
                        InternalEditorUtility.RepaintAllViews();
                        onCompileFinish.OnNext(Unit.Default);
                    }
                }
            }
        }

        #region Observable

        public static IObservable<Unit> OnCompileStartAsObservable()
        {
            return onCompileStart ?? (onCompileStart = new Subject<Unit>());
        }

        public static IObservable<Unit> OnCompileFinishAsObservable()
        {
            return onCompileFinish ?? (onCompileFinish = new Subject<Unit>());
        }

        #endregion
    }
}
