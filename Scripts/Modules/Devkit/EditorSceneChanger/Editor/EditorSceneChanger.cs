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

    public enum SceneChangeState
    {
        None = 0,
        WaitOpenScene,
        WaitSceneChange,
    }

    public static class EditorSceneChanger
    {
        //----- params -----

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

                EditorSceneChangerPrefs.targetScene = targetScenePath;
                EditorApplication.update += SceneChange;

                onEditorSceneChange = new Subject<Unit>();

                return onEditorSceneChange.Select(x => true);
            }

            return Observable.Return(true);
        }

        public static IObservable<Unit> SceneResume()
        {
            if (State != SceneChangeState.None) { return Observable.ReturnUnit(); }

            if (!string.IsNullOrEmpty(EditorSceneChangerPrefs.resumeScene))
            {
                return SceneChange(EditorSceneChangerPrefs.resumeScene)
                    .Do(_ => EditorSceneChangerPrefs.resumeScene = null)
                    .AsUnitObservable();
            }

            return Observable.ReturnUnit();
        }

        private static void SceneChange()
        {
            var scene = EditorSceneManager.GetSceneAt(0);

            switch (State)
            {
                case SceneChangeState.WaitOpenScene:
                    {
                        if (!string.IsNullOrEmpty(EditorSceneChangerPrefs.targetScene))
                        {
                            EditorApplication.LockReloadAssemblies();

                            EditorSceneChangerPrefs.resumeScene = scene.path;
                            EditorSceneManager.OpenScene(EditorSceneChangerPrefs.targetScene);

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
                        if (scene.path == EditorSceneChangerPrefs.targetScene)
                        {
                            State = SceneChangeState.None;

                            EditorSceneChangerPrefs.targetScene = null;

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
    }
}