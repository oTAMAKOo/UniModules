
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.IO;
using System.Threading.Tasks;
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
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-autoUpdate", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-autoUpdate", value); }
            }

			public static DateTime lastUpdate
			{
				get { return ProjectPrefs.Get(typeof(Prefs).FullName + "-lastUpdate", DateTime.MinValue); }
				set { ProjectPrefs.Set(typeof(Prefs).FullName + "-lastUpdate", value); }
			}
        }

        private const int CheckInterval = 1;

        //----- field -----

        private static TextDataAsset embeddedAsset = null;

        private static TextDataAsset distributionAsset = null;

        private static DateTime? nextCheckTime = null;

        //----- property -----

        //----- method -----

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            EditorApplication.update += AutoUpdateTextDataAssetCallback;
        }

        private static async void AutoUpdateTextDataAssetCallback()
        {
			if (Application.isPlaying) { return; }

            if (!Prefs.autoUpdate){ return; }

            if (EditorApplication.isCompiling) { return; }

            if (nextCheckTime.HasValue)
            {
                if (DateTime.Now < nextCheckTime) { return; }
            }

            nextCheckTime = DateTime.Now.AddSeconds(CheckInterval);

            var config = TextDataConfig.Instance;

            //------ Embedded ------

            if (embeddedAsset == null)
            {
                embeddedAsset = TextDataLoader.LoadTextDataAsset(ContentType.Embedded);
            }

            if (embeddedAsset != null)
            {
                await UpdateTextData(embeddedAsset, config.Embedded);
            }

            //------ Distribution ------

            if (distributionAsset == null)
            {
                distributionAsset = TextDataLoader.LoadTextDataAsset(ContentType.Distribution);
            }

            if (distributionAsset != null)
            {
                await UpdateTextData(distributionAsset, config.Distribution);
            }
        }

        private static async Task UpdateTextData(TextDataAsset textDataAsset, TextDataConfig.GenerateAssetSetting setting)
        {
            if (textDataAsset == null){ return; }

            var excelPath = setting.GetExcelPath();

            if (!File.Exists(excelPath)){ return; }

            var lastUpdate = File.GetLastWriteTime(excelPath);
			
            if (lastUpdate <= Prefs.lastUpdate){ return; }

			var languageManager = LanguageManager.Instance;

            var languageInfo = languageManager.Current;

            await TextDataExcel.Export(textDataAsset.ContentType, false);

            TextDataGenerator.Generate(textDataAsset.ContentType, languageInfo);

			Prefs.lastUpdate = lastUpdate;

            UnityConsole.Info("TextData auto updated.");
        }
    }
}
