
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using Unity.Linq;
using System.Linq;
using UniRx;
using Extensions;
using Modules.Devkit.EventHook;
using Modules.TextData.Editor;

namespace Modules.Devkit.CleanComponent
{
    #if UNITY_2018_3_OR_NEWER

    public sealed class PrefabModeCanvasRendererCleaner : CanvasRendererCleaner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            PrefabModeEventHook.OnClosePrefabModeAsObservable().Subscribe(x => ClosePrefabMode(x));
        }

        private static void ClosePrefabMode(PrefabStage prefabStage)
        {
            var gameObjects = prefabStage.prefabContentsRoot.DescendantsAndSelf().ToArray();

            if (!CheckExecute(gameObjects)) { return; }

            TextDataLoader.Reload();

            foreach (var gameObject in gameObjects)
            {
                ModifyComponent(gameObject);
            }

            var prefabRoot = prefabStage.prefabContentsRoot;
            var assetPath = prefabStage.assetPath;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        }
    }

    #endif
}
