
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

            var config = GameTextConfig.Instance;

            var gameTextInfo = GameTextLanguage.Infos.ElementAtOrDefault(GameTextLanguage.Prefs.selection);

            if (gameTextInfo == null) { return; }

            var assetName = gameTextInfo.AssetName;

            // 内包テキスト読み込み.

            gameText.LoadFromResources(assetName);

            // 拡張テキスト読み込み.

            if (config != null && config.UseExternalAseet)
            {
                var assetPath = PathUtility.Combine(config.ExternalAseetFolder, assetName);

                var extendGameTextAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameTextAsset;

                Reflection.InvokePrivateMethod(gameText, "LoadExtend", new object[] { extendGameTextAsset });
            }

            // 適用.
            
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
