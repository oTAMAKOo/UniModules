﻿﻿
using UnityEditor;
using UnityEditor.SceneManagement;
using UniRx;
using System;
using System.Collections;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.EditorSceneChange
{
    public static class EditorSceneChangerPrefs
    {
        public static string targetScene
        {
            get { return ProjectPrefs.GetString("EditorSceneChangerPrefs-targetScene", null); }
            set { ProjectPrefs.SetString("EditorSceneChangerPrefs-targetScene", value); }
        }

        public static string resumeScene
        {
            get { return ProjectPrefs.GetString("EditorSceneChangerPrefs-resumeScene", null); }
            set { ProjectPrefs.SetString("EditorSceneChangerPrefs-resumeScene", value); }
        }
    }

    public static class EditorSceneChanger
    {
        //----- params -----

        private enum State
        {
            None = 0,
            WaitOpenScene,
            WaitSceneChange,
        }

        //----- field -----

        private static State state = State.None;

        //----- property -----

        //----- method -----

        public static IObservable<bool> SceneChange(string targetScenePath)
        {
            if (state != State.None) { return Observable.Return(false); }

            if (string.IsNullOrEmpty(targetScenePath)) { return Observable.Return(false); }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return Observable.Return(false);
            }

            var scene = EditorSceneManager.GetSceneAt(0);

            if (scene.path == targetScenePath)
            {
                state = State.None;
            }
            else
            {
                state = State.WaitOpenScene;

                EditorSceneChangerPrefs.targetScene = targetScenePath;
                
                return Observable.FromMicroCoroutine(() => SceneChange()).Select(x => true);
            }

            return Observable.Return(true);
        }

        public static IObservable<Unit> SceneResume()
        {
            if (state != State.None) { return Observable.ReturnUnit(); }

            if (!string.IsNullOrEmpty(EditorSceneChangerPrefs.resumeScene))
            {
                return SceneChange(EditorSceneChangerPrefs.resumeScene)
                    .Do(_ => EditorSceneChangerPrefs.resumeScene = null)
                    .AsUnitObservable();
            }

            return Observable.ReturnUnit();
        }

        private static IEnumerator SceneChange()
        {
            var loop = true;

            EditorApplication.LockReloadAssemblies();

            while (loop)
            {
                var scene = EditorSceneManager.GetSceneAt(0);

                switch (state)
                {
                    case State.WaitOpenScene:
                        {
                            if (!string.IsNullOrEmpty(EditorSceneChangerPrefs.targetScene))
                            {
                                EditorSceneChangerPrefs.resumeScene = scene.path;
                                EditorSceneManager.OpenScene(EditorSceneChangerPrefs.targetScene);

                                state = State.WaitSceneChange;
                            }
                            else
                            {
                                loop = false;
                            }
                        }
                        break;

                    case State.WaitSceneChange:
                        {
                            if (scene.path == EditorSceneChangerPrefs.targetScene)
                            {
                                loop = false;
                            }
                        }
                        break;
                }

                yield return null;
            }

            state = State.None;

            EditorSceneChangerPrefs.targetScene = null;

            EditorApplication.UnlockReloadAssemblies();
        }
    }
}