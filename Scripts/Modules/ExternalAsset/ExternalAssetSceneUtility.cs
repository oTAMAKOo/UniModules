
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;

#endif

using Object = UnityEngine.Object;

namespace Modules.ExternalAssets
{
    public static class ExternalAssetSceneUtility
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static async UniTask<string[]> LoadScene(string loadPath, LoadSceneMode loadSceneMode = LoadSceneMode.Additive)
        {
            string[] scenes = null;

            #if UNITY_EDITOR

            var externalAsset = ExternalAsset.Instance;

            if (externalAsset.SimulateMode)
            {
                var sceneAsset = await ExternalAsset.LoadAsset<Object>(loadPath, false);

                var sceneAssetPath = AssetDatabase.GetAssetPath(sceneAsset);
                
                EditorSceneManager.LoadSceneInPlayMode(sceneAssetPath, new LoadSceneParameters(loadSceneMode));

                scenes = new string[] { sceneAssetPath };
            }
            else
            {
                scenes = await LoadSceneFromAssetBundle(loadPath, loadSceneMode);
            }

            #else

            scenes = await LoadSceneFromAssetBundle(loadPath, loadSceneMode);

            #endif

            return scenes;
        }

        private static async UniTask<string[]> LoadSceneFromAssetBundle(string loadPath, LoadSceneMode loadSceneMode)
        {
            var scenes = new List<string>();

            var sceneAssetBundle = await ExternalAsset.LoadAsset<AssetBundle>(loadPath, false);

            if (sceneAssetBundle.isStreamedSceneAssetBundle)
            {
                var allScenePaths = sceneAssetBundle.GetAllScenePaths();

                foreach (var scenePath in allScenePaths)
                {
                    await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scenePath, loadSceneMode);

                    scenes.Add(scenePath);
                }
            }
            
            ExternalAsset.UnloadAssetBundle(loadPath);

            return scenes.ToArray();
        }

        public static async UniTask UnLoadScene(string[] scenes)
        {
            foreach (var scene in scenes)
            {
                #if UNITY_EDITOR

                await EditorSceneManager.UnloadSceneAsync(scene);

                #else

                await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);

                #endif
            }
        }
    }
}