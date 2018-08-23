﻿﻿﻿
using System;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UniRx;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.CompileNotice
{
    public static class CompileNotificationPrefs
    {
        public static bool isCompiling
        {
            get { return ProjectPrefs.GetBool("CompileStatsPrefs-isCompiling", false); }
            set { ProjectPrefs.SetBool("CompileStatsPrefs-isCompiling", value); }
        }

        public static string compileStartTime
        {
            get { return ProjectPrefs.GetString("CompileStatsPrefs-compileStartTime", string.Empty); }
            set { ProjectPrefs.SetString("CompileStatsPrefs-compileStartTime", value); }
        }
    }

    public static class CompileNotification
    {
        //----- params -----

        //----- field -----

        private static Subject<Unit> onCompileStart = null;
        private static Subject<Unit> onCompileFinish = null;

        //----- property -----

        
        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            CompileNotificationPrefs.isCompiling = false;
            EditorApplication.update += Update;
        }

        private static void Update()
        {
            if (EditorApplication.isCompiling)
            {
                //------ コンパイル開始 ------

                if (!CompileNotificationPrefs.isCompiling)
                {
                    CompileNotificationPrefs.isCompiling = true;

                    if (onCompileStart != null)
                    {
                        onCompileStart.OnNext(Unit.Default);
                    }
                }
            }
            else
            {
                //------ コンパイル終了 ------

                if(CompileNotificationPrefs.isCompiling)
                {
                    CompileNotificationPrefs.isCompiling = false;

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
