
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Console;

namespace Modules.Bugsnag
{
    public abstract class BugsnagKeyFileGenerateWindow<TInstance, TBugsnagType> : SingletonEditorWindow<TInstance>
        where TInstance : BugsnagKeyFileGenerateWindow<TInstance, TBugsnagType>
        where TBugsnagType : Enum
    {
        //----- params -----

        private static readonly Vector2 WindowSize = new Vector2(580f, 500f);

        private sealed class KeyInfo
        {
            public TBugsnagType BugsnagType { get; set; } 
            public string FileName { get; set; } 
            public string ApiKey { get; set; } 
        }

        //----- field -----

        private IBugsnagManager<TBugsnagType> bugsnagManager = null;

        private AesCryptoKey cryptoKey = null;

        private List<KeyInfo> keyInfos = null;

        private Vector2 scrollPosition = Vector2.zero;

        //----- property -----

        //----- method -----

        public static async UniTask Open(IBugsnagManager<TBugsnagType> bugsnagManager)
        {
            Instance.minSize = WindowSize;
            Instance.maxSize = WindowSize;

            Instance.titleContent = new GUIContent("Generate ApiKeyFile");

            await Instance.Initialize(bugsnagManager);

            Instance.ShowUtility();
        }

        private async UniTask Initialize(IBugsnagManager<TBugsnagType> bugsnagManager)
        {
            this.bugsnagManager = bugsnagManager;

            cryptoKey = await bugsnagManager.GetCryptoKey();

            await LoadApiKeyFile();
        }

        void OnGUI()
        {
            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                foreach (var keyInfo in keyInfos)
                {
                    using (new ContentsScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUILayout.SelectableLabel("BugsnagType : " + keyInfo.BugsnagType, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                            GUILayout.FlexibleSpace();

                            using (new DisableScope(keyInfo.ApiKey.IsNullOrEmpty()))
                            {
                                if (GUILayout.Button("Generate", EditorStyles.miniButton, GUILayout.Width(80f)))
                                {
                                    Generate(keyInfo.BugsnagType, keyInfo.FileName, keyInfo.ApiKey).Forget();
                                }
                            }
                        }
                        
                        var fileDirectory = bugsnagManager.GetFileDirectory(keyInfo.BugsnagType);

                        EditorGUILayout.SelectableLabel("Directory : " + fileDirectory, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                        EditorGUILayout.SelectableLabel("FileName : " + keyInfo.FileName, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                        using (new LabelWidthScope(50f))
                        {
                            keyInfo.ApiKey = EditorGUILayout.TextField("ApiKey", keyInfo.ApiKey);
                        }
                    }
                }

                scrollPosition = scrollViewScope.scrollPosition;
            }
        }

        private async UniTask LoadApiKeyFile()
        {
            keyInfos = new List<KeyInfo>();

            var enumNames = Enum.GetNames(typeof(TBugsnagType));

            foreach (var enumName in enumNames)
            {
                var enumValue = EnumExtensions.FindByName(enumName , default(TBugsnagType));

                var fileName = enumName.GetHash();

                var fileDirectory = bugsnagManager.GetFileDirectory(enumValue);

                var filePath = PathUtility.Combine(fileDirectory, fileName);

                if (!File.Exists(filePath)){ continue; }

                var keyData = await MessagePackFileUtility.Read<BugsnagApiKeyData>(filePath, cryptoKey);

                if (keyData == null) { continue; }

                var keyInfo = new KeyInfo()
                {
                    BugsnagType = enumValue,
                    FileName = fileName,
                    ApiKey = keyData.apiKey,
                };

                keyInfos.Add(keyInfo);
            }
        }

        private async UniTask Generate(TBugsnagType keyType, string fileName, string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)){ return; }

            var directory = bugsnagManager.GetFileDirectory(keyType);

            var assetPath = PathUtility.Combine(directory, fileName);

            var filePath = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);

            var data = new BugsnagApiKeyData()
            {
                apiKey = apiKey,
            };

            await MessagePackFileUtility.Write(filePath, data, cryptoKey);

            if (File.Exists(filePath))
            {
                AssetDatabase.ImportAsset(assetPath);
            }

            UnityConsole.Info($"Generate Bugsnag ApiKeyFile.\n{assetPath}");
        }
    }
}