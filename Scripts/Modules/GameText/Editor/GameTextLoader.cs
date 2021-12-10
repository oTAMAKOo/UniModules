
using UnityEngine;
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
            SetupCryptoKey();

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

        public static void SetupCryptoKey()
        {
            var gameText = GameText.Instance;

            if (gameText == null){ return; }

            var config = GameTextConfig.Instance;

            if (config == null) { return; }

            // 暗号化キー設定.

            gameText.SetCryptoKey(config.CryptoKey, config.CryptoIv);
        }

        public static void Reload()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

            var gameText = GameText.Instance;

            if (gameText == null){ return; }

            var config = GameTextConfig.Instance;

            if (config == null) { return; }
            
            // 内包テキスト読み込み.

            var embeddedAsset = LoadGameTextAsset(ContentType.Embedded);

            gameText.LoadEmbedded(embeddedAsset);

            // 配信テキスト読み込み.

            if (config != null && config.Distribution.Enable)
            {
                var distributionAsset = LoadGameTextAsset(ContentType.Distribution);

                gameText.AddContents(distributionAsset);
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

        public static GameTextAsset LoadGameTextAsset(ContentType contentType)
        {
            GameTextAsset gameTextAsset = null;

            var gameText = GameText.Instance;

            var config = GameTextConfig.Instance;

            var languageInfo = GameTextLanguage.GetCurrentInfo();

            if (languageInfo == null) { return null; }

            var identifier = languageInfo.Identifier;

            var assetFolderName = gameText.GetAssetFolderName();

            var assetFileName = GameText.GetAssetFileName(identifier);

            switch (contentType)
            {
                case ContentType.Embedded:
                    {
                        var resourcesPath = PathUtility.Combine(assetFolderName, assetFileName);

                        var path = PathUtility.GetPathWithoutExtension(resourcesPath);

                        gameTextAsset = Resources.Load<GameTextAsset>(path);
                    }
                    break;

                case ContentType.Distribution:
                    {
                        if (config != null && config.Distribution.Enable)
                        {
                            var aseetFolderPath = config.Distribution.AseetFolderPath;

                            var assetPath = PathUtility.Combine(new string[] { aseetFolderPath, assetFolderName, assetFileName });
                    
                            gameTextAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameTextAsset;
                        }
                    }
                    break;
            }

            return gameTextAsset;
        }
    }
}
