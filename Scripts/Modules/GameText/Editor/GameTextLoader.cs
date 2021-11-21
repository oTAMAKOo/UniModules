
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

            GameTextLanguage.Info gameTextInfo = null;

            if (1 < GameTextLanguage.Infos.Length)
            {
                var selection = GameTextLanguage.Prefs.selection;

                gameTextInfo = GameTextLanguage.Infos.ElementAtOrDefault(selection);
            }
            else
            {
                gameTextInfo = GameTextLanguage.Infos.FirstOrDefault();
            }

            if (gameTextInfo == null) { return; }

            var identifier = gameTextInfo.Identifier;

            var assetFolderName = gameText.GetAssetFolderName();

            // 内包テキスト読み込み.

            if (config != null)
            {
                var assetFileName = GameText.GetAssetFileName(identifier);

                var resourcesPath = PathUtility.Combine(assetFolderName, assetFileName);

                gameText.LoadEmbedded(resourcesPath);
            }

            // 配信テキスト読み込み.

            if (config != null && config.Distribution.Enable)
            {
                var assetFileName = GameText.GetAssetFileName(identifier);

                var aseetFolderPath = config.Distribution.AseetFolderPath;

                var assetPath = PathUtility.Combine(new string[] { aseetFolderPath, assetFolderName, assetFileName });
                
                var distributionAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameTextAsset;

                if (distributionAsset != null)
                {
                    gameText.AddContents(distributionAsset);
                }
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
