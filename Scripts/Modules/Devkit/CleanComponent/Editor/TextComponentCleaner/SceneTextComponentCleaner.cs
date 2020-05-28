
using System.Linq;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Linq;
using TMPro;
using UniRx;
using Extensions;
using Modules.Devkit.EventHook;
using Modules.GameText.Editor;

namespace Modules.Devkit.CleanComponent
{
    public sealed class SceneTextComponentCleaner : TextComponentCleaner
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            CurrentSceneSaveHook.OnSaveSceneAsObservable().Subscribe(x => Clean(x));
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

            if (rootGameObjects.IsEmpty()) { return; }

            var gameObjects = rootGameObjects.DescendantsAndSelf().ToArray();

            var textComponents = GetComponentInfos<Text>(gameObjects);

            var textMeshProComponents = GetComponentInfos<TextMeshProUGUI>(gameObjects);

            // 開発用テキストクリア.

            CleanDevelopmentText(textComponents);

            CleanDevelopmentText(textMeshProComponents);

            // 実テキストをクリア.

            if (Prefs.autoClean)
            {
                GameTextLoader.Reload();

                var check = false;

                check |= CheckExecute(textComponents, t => t.text);

                check |= CheckExecute(textMeshProComponents, t => t.text);

                if (check && ConfirmExecute())
                {
                    ModifyTextComponent(textComponents, t => string.IsNullOrEmpty(t.text), t => t.text = string.Empty);

                    ModifyTextComponent(textMeshProComponents, t => string.IsNullOrEmpty(t.text), t => t.text = string.Empty);
                }
            }

            // 保存後に開発テキストを再適用.

            EditorApplication.delayCall += () =>
            {
                ApplyDevelopmentText(textComponents);

                ApplyDevelopmentText(textMeshProComponents);
            };
        }
    }
}
