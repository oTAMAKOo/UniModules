
using UnityEditor;
using UnityEditor.Callbacks;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.GameText.Components;
using UnityEngine;

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
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

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
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

            var gameText = GameText.Instance;

            var config = GameTextConfig.Instance;

            var gameTextInfo = GameTextLanguage.Infos.ElementAtOrDefault(GameTextLanguage.Prefs.selection);

            if (gameTextInfo == null) { return; }

            var identifier = gameTextInfo.Identifier;

            var assetFolderName = gameText.GetAssetFolderName();

            // 内包テキスト読み込み.

            if (config != null)
            {
                var assetFileName = GameText.GetAssetFileName(GameText.AssetType.BuiltIn, identifier);

                var resourcesPath = PathUtility.Combine(assetFolderName, assetFileName);

                gameText.LoadBuiltInAsset(resourcesPath);
            }

            // 更新テキスト読み込み.

            if (config != null && config.UpdateGameText.Enable)
            {
                var assetFileName = GameText.GetAssetFileName(GameText.AssetType.Update, identifier);

                var aseetFolderPath = config.UpdateGameText.AseetFolderPath;

                var assetPath = PathUtility.Combine(new string[] { aseetFolderPath, assetFolderName, assetFileName });

                var updateGameTextAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameTextAsset;

                gameText.ImportAsset(updateGameTextAsset);
            }

            // 拡張テキスト読み込み.

            if (config != null && config.ExtendGameText.Enable)
            {
                var assetFileName = GameText.GetAssetFileName(GameText.AssetType.Extend, identifier);

                var aseetFolderPath = config.ExtendGameText.AseetFolderPath;

                var assetPath = PathUtility.Combine(new string[] { aseetFolderPath, assetFolderName, assetFileName });
                
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
