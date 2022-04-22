
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.TextData.Components;

namespace Modules.TextData.Editor
{
    public static class TextDataLoader
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public static bool IsLoaded { get; private set; }

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            SetupCryptoKey();
        }

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            SetupCryptoKey();

            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

            var frameCount = 0;

            EditorApplication.CallbackFunction reloadTextData = null;

            reloadTextData = () =>
            {
                if (frameCount < 30)
                {
                    frameCount++;
                    return;
                }

                Reload();

                EditorApplication.update -= reloadTextData;
            };

            EditorApplication.update += reloadTextData;
        }

        public static void SetupCryptoKey()
        {
            var textData = TextData.Instance;

            if (textData == null){ return; }

            var config = TextDataConfig.Instance;

            if (config == null) { return; }

            // 暗号化キー設定.

            textData.SetCryptoKey(config.CryptoKey, config.CryptoIv);
        }

        public static void Reload()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

            var textData = TextData.Instance;

            if (textData == null){ return; }

            var config = TextDataConfig.Instance;

            if (config == null) { return; }
            
            // 内包テキスト読み込み.

            var embeddedAsset = LoadTextDataAsset(ContentType.Embedded);

            textData.LoadEmbedded(embeddedAsset);

            // 配信テキスト読み込み.

            if (config != null && config.Distribution.Enable)
            {
                var distributionAsset = LoadTextDataAsset(ContentType.Distribution);

                textData.AddContents(distributionAsset);
            }

            // 適用.
            
            var gameObjects = UnityEditorUtility.FindAllObjectsInHierarchy();

            foreach (var gameObject in gameObjects)
            {
                var setter = gameObject.GetComponent<TextSetter>();

                if (setter != null)
                {
                    Reflection.InvokePrivateMethod(setter, "ImportText");
                }
            }

            IsLoaded = true;
        }

        public static TextDataAsset LoadTextDataAsset(ContentType contentType)
        {
            TextDataAsset textDataAsset = null;

            var textData = TextData.Instance;

			var config = TextDataConfig.Instance;

			var languageManager = LanguageManager.Instance;

            var languageInfo = languageManager.Current;

            if (languageInfo == null) { return null; }

            var identifier = languageInfo.Identifier;

            var assetFolderName = textData.GetAssetFolderName();

            var assetFileName = TextData.GetAssetFileName(identifier);

            switch (contentType)
            {
                case ContentType.Embedded:
                    {
                        var resourcesPath = PathUtility.Combine(assetFolderName, assetFileName);

                        var path = PathUtility.GetPathWithoutExtension(resourcesPath);

                        textDataAsset = Resources.Load<TextDataAsset>(path);
                    }
                    break;

                case ContentType.Distribution:
                    {
                        if (config != null && config.Distribution.Enable)
                        {
                            var aseetFolderPath = config.Distribution.AseetFolderPath;

                            var assetPath = PathUtility.Combine(new string[] { aseetFolderPath, assetFolderName, assetFileName });
                    
                            textDataAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as TextDataAsset;
                        }
                    }
                    break;
            }

            return textDataAsset;
        }
    }
}
