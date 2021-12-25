
using System;
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

            // 開発用テキストクリア.

            changed |= CleanDevelopmentText(textComponents);

            changed |= CleanDevelopmentText(textMeshProComponents);

            // 実テキストをクリア.

            if (Prefs.autoClean)
            {
                GameTextLoader.Reload();

                var check = false;

                check |= CheckExecute(textComponents, t => t.text);

                check |= CheckExecute(textMeshProComponents, t => t.text);

                if (check && ConfirmExecute())
                {
                    changed |= ModifyTextComponent(textComponents, t => string.IsNullOrEmpty(t.text), t => t.text = string.Empty);

                    changed |= ModifyTextComponent(textMeshProComponents, t => string.IsNullOrEmpty(t.text), t => t.text = string.Empty);
                }
            }

            // 変更があったら保存.

            if (changed)
            {
                var prefabRoot = prefabStage.prefabContentsRoot;
                var assetPath = prefabStage.assetPath;
                
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);

                // 保存後に開発テキストを再適用.

                ApplyDevelopmentText(textComponents);

                ApplyDevelopmentText(textMeshProComponents);
            }
        }
    }

    #endif
}
