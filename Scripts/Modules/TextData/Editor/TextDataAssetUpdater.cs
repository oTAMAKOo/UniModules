
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.IO;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.Console;
using Modules.Devkit.Prefs;
using Modules.TextData.Components;

namespace Modules.TextData.Editor
{
    public sealed class TextDataAssetUpdater
    {
        //----- params -----

        public static class Prefs
        {
            public static bool autoUpdate
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-autoUpdate", true); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-autoUpdate", value); }
            }

            public static DateTime internalLastUpdate
            {
                get { return ProjectPrefs.Get(typeof(Prefs).FullName + "-internalLastUpdate", DateTime.MinValue); }
                set { ProjectPrefs.Set(typeof(Prefs).FullName + "-internalLastUpdate", value); }
            }

            public static DateTime externalLastUpdate
            {
                get { return ProjectPrefs.Get(typeof(Prefs).FullName + "-externalLastUpdate", DateTime.MinValue); }
                set { ProjectPrefs.Set(typeof(Prefs).FullName + "-externalLastUpdate", value); }
            }
        }

        private const float CheckInterval = 1f;

        //----- field -----

        private static bool running = false;

        private static TextDataAsset internalAsset = null;

        private static TextDataAsset externalAsset = null;

        private static DateTime? nextCheckTime = null;

        //----- property -----

        //----- method -----

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
            EditorApplication.update += AutoUpdateTextDataAssetCallback;
        }

        private static void AutoUpdateTextDataAssetCallback()
        {
            AutoUpdateTextDataAsset().Forget();
        }

        private static async UniTask AutoUpdateTextDataAsset()
        {
            if (nextCheckTime.HasValue)
            {
                if (DateTime.Now < nextCheckTime) { return; }
            }

            nextCheckTime = DateTime.Now.AddSeconds(CheckInterval);

            if (IsAutoUpdateSkip()){ return; }

            // 選択中言語.

            var languageInfo = TextDataLoader.GetCurrentLanguage();

            if (languageInfo == null){ return; }

            // テキスト生成.

            var config = TextDataConfig.Instance;

            var autoUpdate = Prefs.autoUpdate;

            //------ Internal ------

            if (config.Internal != null)
            {
                if (internalAsset == null)
                {
                    internalAsset = TextDataLoader.LoadTextDataAsset(TextType.Internal);

                    if (internalAsset == null)
                    {
                        internalAsset = GenerateTextDataAsset(TextType.Internal);
                    }
                }

                if (autoUpdate && internalAsset != null)
                {
                    var source = config.Internal.Source;

                    await UpdateTextDataAsset(internalAsset, source, Prefs.internalLastUpdate, x => Prefs.internalLastUpdate = x);
                }
            }

            //------ Distribution ------

            if (config.EnableExternal)
            {
                if (externalAsset == null)
                {
                    externalAsset = TextDataLoader.LoadTextDataAsset(TextType.External);

                    if (externalAsset == null)
                    {
                        externalAsset = GenerateTextDataAsset(TextType.External);
                    }
                }

                if (autoUpdate && externalAsset != null)
                {
                    var source = config.External.Source;

                    await UpdateTextDataAsset(externalAsset, source, Prefs.externalLastUpdate, x => Prefs.externalLastUpdate = x);
                }
            }
        }

        private static bool IsAutoUpdateSkip()
        {
            if (Application.isPlaying) { return true; }

            if (EditorApplication.isPlayingOrWillChangePlaymode){ return true; }

            if (EditorApplication.isCompiling) { return true; }

            if (EditorApplication.isUpdating){ return true; }

            if (TextDataExcel.Importing || TextDataExcel.Exporting) { return true; }

            return false;
        }

        private static TextDataAsset GenerateTextDataAsset(TextType textType)
        {
            var languageManager = LanguageManager.Instance;

            var languageInfo = languageManager.Current;

            TextDataGenerator.Generate(textType, languageInfo, true);

            var textDataAsset = TextDataLoader.LoadTextDataAsset(textType);

            UnityConsole.Info("TextData auto generated.");

            return textDataAsset;
        }

        private static async Task UpdateTextDataAsset(TextDataAsset textDataAsset, TextDataSource[] targets, DateTime lastUpdate, Action<DateTime> onUpdate)
        {
            if (running) { return; }

            if (textDataAsset == null){ return; }

            if (targets == null){ return; }

            running = true;

            try
            {
                DateTime? updateTime = null;

                foreach (var target in targets)
                {
                    var excelPath = target.GetExcelPath();

                    if (!File.Exists(excelPath)){ continue; }

                    var lastWriteTime = File.GetLastWriteTime(excelPath);
            
                    if (lastWriteTime <= lastUpdate){ continue; }

                    await TextDataExcel.Export(target, false);

                    if (updateTime.HasValue)
                    {
                        if (updateTime.Value < lastWriteTime)
                        {
                            updateTime = lastWriteTime;
                        }
                    }
                    else
                    {
                        updateTime = lastWriteTime;
                    }
                }

                if (updateTime.HasValue)
                {
                    var languageManager = LanguageManager.Instance;

                    var languageInfo = languageManager.Current;

                    TextDataGenerator.Generate(textDataAsset.Type, languageInfo);

                    onUpdate.Invoke(updateTime.Value);

                    UnityConsole.Info("TextData auto updated.");
                }
            }
            finally
            {
                running = false;
            }
        }
    }
}
