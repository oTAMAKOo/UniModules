
using UnityEditor;
using System.Linq;
using UniRx;
using Extensions;
using Unity.Linq;
using Modules.Devkit.EventHook;

#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif 

namespace Modules.Devkit.CleanComponent
{
    public sealed class PrefabModeParticleComponentCleaner : ParticleComponentCleaner
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

            foreach (var gameObject in gameObjects)
            {
                ModifyParticleSystemComponent(gameObject);
            }

            var prefabRoot = prefabStage.prefabContentsRoot;
            var assetPath = prefabStage.assetPath;

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
        }
    }
}
