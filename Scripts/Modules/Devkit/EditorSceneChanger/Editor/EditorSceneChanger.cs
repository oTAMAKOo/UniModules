
using UnityEditor;
using UnityEditor.SceneManagement;
using UniRx;
using System;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.EditorSceneChange
{
    public static class EditorSceneChangerPrefs
    {
        public static string lastScene
        {
            get { return ProjectPrefs.GetString("EditorSceneChangerPrefs-lastScene", null); }
            set { ProjectPrefs.SetString("EditorSceneChangerPrefs-lastScene", value); }
        }

        public static string waitScene
        {
            get { return ProjectPrefs.GetString("EditorSceneChangerPrefs-waitScene", null); }
            set { ProjectPrefs.SetString("EditorSceneChangerPrefs-waitScene", value); }
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
        private static int waitCount = 0;

        private static Subject<Unit> onEditorSceneChange = null;

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
                waitCount = 0;
                EditorSceneChangerPrefs.waitScene = targetScenePath;
                EditorApplication.update += SceneChange;

                onEditorSceneChange = new Subject<Unit>();

                return onEditorSceneChange
                    .Select(x => true);
            }

            return Observable.Return(true);
        }

        public static IObservable<Unit> SceneResume(Action onResumeComplete = null)
        {
            if (state != State.None) { return Observable.ReturnUnit(); }

            if (!string.IsNullOrEmpty(EditorSceneChangerPrefs.lastScene))
            {
                return SceneChange(EditorSceneChangerPrefs.lastScene)
                    .Do(
                        _ =>
                        {
                            if (onResumeComplete != null)
                            {
                                onResumeComplete();
                            }
                            EditorSceneChangerPrefs.lastScene = null;
                        })
                    .AsUnitObservable();
            }

            return Observable.ReturnUnit();
        }

        private static void SceneChange()
        {
            var scene = EditorSceneManager.GetSceneAt(0);

            switch (state)
            {
                case State.WaitOpenScene:
                    if (20 < waitCount++)
                    {
                        state = State.WaitSceneChange;
                        EditorSceneChangerPrefs.lastScene = scene.path;
                        EditorSceneManager.OpenScene(EditorSceneChangerPrefs.waitScene);
                    }
                    break;

                case State.WaitSceneChange:
                    if (scene.path == EditorSceneChangerPrefs.waitScene)
                    {
                        state = State.None;
                        EditorSceneChangerPrefs.waitScene = null;
                        onEditorSceneChange.OnNext(Unit.Default);
                        onEditorSceneChange.OnCompleted();
                        onEditorSceneChange = null;
                        EditorApplication.update -= SceneChange;
                    }
                    break;
            }
        }
    }
}
