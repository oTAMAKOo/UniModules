
using UnityEngine.SceneManagement;
using System;
using UniRx;

namespace Modules.Devkit.EventHook
{
    public sealed class CurrentSceneSaveHook : UnityEditor.AssetModificationProcessor
    {
        //----- params -----

        //----- field -----

        private static Subject<string> onSaveScene = null;

        //----- property -----

        //----- method -----

        public static string[] OnWillSaveAssets(string[] paths)
        {
            var currentScenePath = SceneManager.GetActiveScene().path;

            foreach (string path in paths)
            {
                if (path.Contains(".unity"))
                {
                    if (currentScenePath == path)
                    {
                        if (onSaveScene != null)
                        {
                            onSaveScene.OnNext(path);
                        }
                    }
                }
            }

            return paths;
        }

        public static IObservable<string> OnSaveSceneAsObservable()
        {
            return onSaveScene ?? (onSaveScene = new Subject<string>());
        }
    }
}
