﻿﻿
using UnityEditor;
using UnityEditor.SceneManagement;
using UniRx;
using System;
using System.Collections;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.EditorSceneChange
{
    public enum SceneChangeState
    {
        None = 0,
        WaitOpenScene,
        WaitSceneChange,
    }

    public static class EditorSceneChanger
    {
        //----- params -----

        public static class Prefs
        {
            public static string targetScene
            {
                get { return ProjectPrefs.GetString(typeof(Prefs).FullName + "-targetScene", null); }
                set { ProjectPrefs.SetString(typeof(Prefs).FullName + "-targetScene", value); }
            }

            public static string resumeScene
            {
                get { return ProjectPrefs.GetString(typeof(Prefs).FullName + "-resumeScene", null); }
                set { ProjectPrefs.SetString(typeof(Prefs).FullName + "-resumeScene", value); }
            }
        }

        //----- field -----
        
        private static Subject<Unit> onEditorSceneChange = null;

        //----- property -----

        public static SceneChangeState State { get; private set; }

        //----- method -----

        public static IObservable<bool> SceneChange(string targetScenePath)
        {
            if (State != SceneChangeState.None) { return Observable.Return(false); }

            if (string.IsNullOrEmpty(targetScenePath)) { return Observable.Return(false); }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return Observable.Return(false);
            }

            var scene = EditorSceneManager.GetSceneAt(0);

            if (scene.path == targetScenePath)
            {
                State = SceneChangeState.None;
            }
            else
            {
                State = SceneChangeState.WaitOpenScene;

                Prefs.targetScene = targetScenePath;
                EditorApplication.update += SceneChange;

                onEditorSceneChange = new Subject<Unit>();

                return onEditorSceneChange.Select(x => true);
            }

            return Observable.Return(true);
        }

        public static IObservable<Unit> SceneResume()
        {
            if (State != SceneChangeState.None) { return Observable.ReturnUnit(); }

            if (!string.IsNullOrEmpty(Prefs.resumeScene))
            {
                return SceneChange(Prefs.resumeScene)
                    .Do(_ => Prefs.resumeScene = null)
                    .AsUnitObservable();
            }

            return Observable.ReturnUnit();
        }

        private static void SceneChange()
        {
            var scene = EditorSceneManager.GetSceneAt(0);

            try
            {
                switch (State)
                {
                    case SceneChangeState.WaitOpenScene:
                        {
                            if (!string.IsNullOrEmpty(Prefs.targetScene))
                            {
                                EditorApplication.LockReloadAssemblies();

                                Prefs.resumeScene = scene.path;

                                EditorSceneManager.OpenScene(Prefs.targetScene);

                                State = SceneChangeState.WaitSceneChange;
                            }
                            else
                            {
                                State = SceneChangeState.None;

                                EditorApplication.update -= SceneChange;
                            }
                        }
                        break;

                    case SceneChangeState.WaitSceneChange:
                        {
                            if (scene.path == Prefs.targetScene)
                            {
                                State = SceneChangeState.None;

                                Prefs.targetScene = null;

                                onEditorSceneChange.OnNext(Unit.Default);
                                onEditorSceneChange.OnCompleted();
                                onEditorSceneChange = null;

                                EditorApplication.update -= SceneChange;

                                EditorApplication.UnlockReloadAssemblies();
                            }
                        }
                        break;
                }
            }
            catch
            {
                State = SceneChangeState.None;

                EditorApplication.update -= SceneChange;

                EditorApplication.UnlockReloadAssemblies();
            }
        }
    }
}
