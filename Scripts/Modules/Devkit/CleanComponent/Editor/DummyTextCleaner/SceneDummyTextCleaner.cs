
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.EventHook;

namespace Modules.Devkit.CleanComponent
{
    public sealed class SceneDummyTextCleaner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            void OnSaveScene(string sceneAssetPath)
            {
                Clean(sceneAssetPath);
                        
                ReApply(sceneAssetPath).Forget();
            }

            CurrentSceneSaveHook.OnSaveSceneAsObservable().Subscribe(x =>OnSaveScene(x));
        }

        public static void Clean()
        {
            var activeScene = EditorSceneManager.GetActiveScene();

            Clean(activeScene.path);
        }

        private static void Clean(string sceneAssetPath)
        {
            var activeScene = EditorSceneManager.GetActiveScene();

            if (activeScene.path != sceneAssetPath) { return; }

            var rootGameObjects = activeScene.GetRootGameObjects();

            foreach (var rootGameObject in rootGameObjects)
            {
                DummyTextCleaner.ModifyComponents(rootGameObject);
            }
        }

        private static async UniTask ReApply(string sceneAssetPath)
        {
            var filePath = UnityPathUtility.ConvertAssetPathToFullPath(sceneAssetPath);

            ulong? prevLastWriteTime = null;

            while (true)
            {
                var fileInfo = new FileInfo(filePath);

                var lastWriteTime = fileInfo.LastWriteTimeUtc.ToUnixTime();

                if (prevLastWriteTime.HasValue)
                {
                    if (prevLastWriteTime != lastWriteTime){ break; }
                }

                prevLastWriteTime = lastWriteTime;

                await UniTask.NextFrame();
            }

            EditorApplication.delayCall += () =>
            {
                var activeScene = EditorSceneManager.GetActiveScene();

                if (activeScene.path != sceneAssetPath) { return; }

                var rootGameObjects = activeScene.GetRootGameObjects();

                foreach (var rootGameObject in rootGameObjects)
                {
                    DummyTextCleaner.ReApply(rootGameObject);
                }
            };
        }
    }
}