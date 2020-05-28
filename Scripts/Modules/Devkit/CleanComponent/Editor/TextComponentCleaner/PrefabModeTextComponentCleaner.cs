
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.Experimental.SceneManagement;
using Unity.Linq;
using System.Linq;
using TMPro;
using UniRx;
using Modules.Devkit.EventHook;
using Modules.GameText.Editor;

namespace Modules.Devkit.CleanComponent
{
    #if UNITY_2018_3_OR_NEWER

    public sealed class PrefabModeTextComponentCleaner : TextComponentCleaner
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
            var changed = false;

            var gameObjects = prefabStage.prefabContentsRoot.DescendantsAndSelf().ToArray();

            var textComponents = GetComponentInfos<Text>(gameObjects);

            var textMeshProComponents = GetComponentInfos<TextMeshProUGUI>(gameObjects);

            changed |= CleanDevelopmentText(textComponents, textMeshProComponents);

            if (Prefs.autoClean)
            {
                GameTextLoader.Reload();

                if (!CheckExecute(textComponents, textMeshProComponents)) { return; }

                changed |= ModifyTextComponent(textComponents, textMeshProComponents);
            }

            if (changed)
            {
                var prefabRoot = prefabStage.prefabContentsRoot;
                var assetPath = prefabStage.prefabAssetPath;
                
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            }
        }
    }

    #endif
}
