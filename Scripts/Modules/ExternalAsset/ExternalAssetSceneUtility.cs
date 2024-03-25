
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions;
using UniRx;
using Modules.UniRxExtension;

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

        public sealed class SceneLoadAsyncHandler : AsyncHandler
        {
            public UnityEngine.SceneManagement.Scene scene;

            public LoadSceneMode mode;
        }

        //----- field -----

        private static Subject<SceneLoadAsyncHandler> onLoadScene = null;

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
                
                var scene = EditorSceneManager.LoadSceneInPlayMode(sceneAssetPath, new LoadSceneParameters(loadSceneMode));

                scenes = new string[] { sceneAssetPath };

                if (onLoadScene != null)
                {
                    var asyncHandler = new SceneLoadAsyncHandler()
                    {
                        scene = scene,
                        mode = loadSceneMode,
                    };

                    onLoadScene.OnNext(asyncHandler);

                    await asyncHandler.Wait();
                }
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
                    UnityEngine.SceneManagement.Scene? scene = null;

                    LoadSceneMode mode = default;

                    void sceneLoaded(UnityEngine.SceneManagement.Scene _scene, LoadSceneMode _mode)
                    {
                        if (!_scene.IsValid()){ return; }

                        scene = _scene;
                        mode = _mode;
                    }

                    UnityEngine.SceneManagement.SceneManager.sceneLoaded += sceneLoaded;

                    try
                    {
                        var op = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(scenePath, loadSceneMode);

                        op.allowSceneActivation = false;

                        while (op.progress < 0.9f)
                        {
                            await UniTask.NextFrame();
                        }

                        op.allowSceneActivation = true;

                        while (!op.isDone)
                        {
                            await UniTask.NextFrame();
                        }
                    }
                    finally
                    {
                        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= sceneLoaded;
                    }

                    if (scene.HasValue)
                    {
                        scenes.Add(scenePath);

                        if (onLoadScene != null)
                        {
                            var asyncHandler = new SceneLoadAsyncHandler()
                            {
                                scene = scene.Value,
                                mode = mode,
                            };

                            onLoadScene.OnNext(asyncHandler);

                            await asyncHandler.Wait();
                        }
                    }
                }
            }

            ExternalAsset.UnloadAssetBundle(loadPath);

            return scenes.ToArray();
        }

        public static async UniTask UnLoadScene(string[] scenes)
        {
            foreach (var scene in scenes)
            {
                await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
            }
        }

        public static IObservable<SceneLoadAsyncHandler> OnLoadSceneAsObservable()
        {
            return onLoadScene ?? (onLoadScene = new Subject<SceneLoadAsyncHandler>());
        }
    }
}