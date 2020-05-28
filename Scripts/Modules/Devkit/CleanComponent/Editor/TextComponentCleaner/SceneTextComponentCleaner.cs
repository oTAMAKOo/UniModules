
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

            CleanDevelopmentText(textComponents, textMeshProComponents);

            if (Prefs.autoClean)
            {
                GameTextLoader.Reload();

                if (!CheckExecute(textComponents, textMeshProComponents)) { return; }

                ModifyTextComponent(textComponents, textMeshProComponents);
            }

            EditorApplication.delayCall += () =>
            {
                ApplyDevelopmentText(textComponents);

                ApplyDevelopmentText(textMeshProComponents);
            };
        }
    }
}
