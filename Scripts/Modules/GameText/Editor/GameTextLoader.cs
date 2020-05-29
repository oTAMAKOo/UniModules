
using UnityEditor;
using UnityEditor.Callbacks;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.GameText.Components;

namespace Modules.GameText.Editor
{
    public static class GameTextLoader
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public static bool IsLoaded { get; private set; }

        //----- method -----

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            var frameCount = 0;

            EditorApplication.CallbackFunction reloadGameText = null;

            reloadGameText = () =>
            {
                if (frameCount < 30)
                {
                    frameCount++;
                    return;
                }

                Reload();

                EditorApplication.update -= reloadGameText;
            };


            EditorApplication.update += reloadGameText;
        }

        public static void Reload()
        {
            var gameText = GameText.Instance;

            var gameTextInfo = GameTextLanguage.Infos.ElementAtOrDefault(GameTextLanguage.Prefs.selection);

            if (gameTextInfo == null) { return; }

            gameText.LoadFromResources(gameTextInfo.AssetName);
            
            var gameObjects = UnityEditorUtility.FindAllObjectsInHierarchy();

            foreach (var gameObject in gameObjects)
            {
                var setter = gameObject.GetComponent<GameTextSetter>();

                if (setter != null)
                {
                    Reflection.InvokePrivateMethod(setter, "ImportText");
                }
            }

            IsLoaded = true;
        }
    }
}
