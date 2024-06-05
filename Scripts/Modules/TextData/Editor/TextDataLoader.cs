
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
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
        private static void OnDidReloadScripts()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += OnDidReloadScripts;
                return;
            }
 
            EditorApplication.delayCall += OnAfterDidReloadScripts;
        }

        private static void OnAfterDidReloadScripts()
        {
            SetupCryptoKey();

            var frameCount = 0;

            void ReloadTextDataCallback()
            {
                if (Application.isPlaying) { return; }

                if (frameCount < 30)
                {
                    frameCount++;
                    return;
                }

                Reload();

                EditorApplication.update -= ReloadTextDataCallback;
            }

            EditorApplication.update += ReloadTextDataCallback;
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

            var internalAsset = LoadTextDataAsset(TextType.Internal);

            textData.LoadEmbedded(internalAsset);

            // 配信テキスト読み込み.

            if (config.EnableExternal)
            {
                var externalAsset = LoadTextDataAsset(TextType.External);

                textData.AddContents(externalAsset);
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

        public static TextDataAsset LoadTextDataAsset(TextType type)
        {
            TextDataAsset textDataAsset = null;

            var textData = TextData.Instance;

            var config = TextDataConfig.Instance;

            var languageManager = LanguageManager.Instance;

            var languageInfo = languageManager.Current;

            if (languageInfo == null) { return null; }

            var identifier = languageInfo.Identifier;

            var assetFolderLocalPath = textData.AssetFolderLocalPath;

            var assetFileName = TextData.GetAssetFileName(identifier);

            switch (type)
            {
                case TextType.Internal:
                    {
                        var resourcesPath = PathUtility.Combine(assetFolderLocalPath, assetFileName);

                        var path = PathUtility.GetPathWithoutExtension(resourcesPath);

                        textDataAsset = Resources.Load<TextDataAsset>(path);
                    }
                    break;

                case TextType.External:
                    {
                        if (config.EnableExternal)
                        {
                            var aseetFolderPath = config.External.AseetFolderPath;

                            var assetPath = PathUtility.Combine(new string[] { aseetFolderPath, assetFolderLocalPath, assetFileName });
                    
                            textDataAsset = AssetDatabase.LoadMainAssetAtPath(assetPath) as TextDataAsset;
                        }
                    }
                    break;
            }

            return textDataAsset;
        }
    }
}
