
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions;
using UniRx;

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

        private static Subject<UnityEngine.SceneManagement.Scene> onLoadScene = null;

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
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded += SceneLoaded;

                    try
                    {
                        await UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scenePath, loadSceneMode);
                    }
                    finally
                    {
                        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= SceneLoaded;
                    }

                    scenes.Add(scenePath);
                }
            }

            ExternalAsset.UnloadAssetBundle(loadPath);

            return scenes.ToArray();
        }

        private static void SceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
        {
            if (!scene.IsValid()){ return; }

            if (onLoadScene != null)
            {
                onLoadScene.OnNext(scene);
            }
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

        public static IObservable<UnityEngine.SceneManagement.Scene> OnLoadSceneAsObservable()
        {
            return onLoadScene ?? (onLoadScene = new Subject<UnityEngine.SceneManagement.Scene>());
        }
    }
}