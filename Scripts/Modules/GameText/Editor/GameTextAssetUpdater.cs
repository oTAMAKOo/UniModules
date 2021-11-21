
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Extensions;
using Modules.Devkit.Console;
using Modules.Devkit.Prefs;
using Modules.GameText.Components;

namespace Modules.GameText.Editor
{
    public sealed class GameTextAssetUpdater
    {
        //----- params -----

        public static class Prefs
        {
            public static bool autoUpdate
            {
                get { return ProjectPrefs.GetBool("GameTextAssetUpdaterPrefs-autoUpdate", false); }
                set { ProjectPrefs.SetBool("GameTextAssetUpdaterPrefs-autoUpdate", value); }
            }
        }

        private const int CheckInterval = 1;

        //----- field -----

        private static GameTextAsset embeddedAsset = null;

        private static GameTextAsset distributionAsset = null;

        private static DateTime? nextCheckTime = null;

        //----- property -----

        //----- method -----

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) { return; }

            EditorApplication.update += AutoUpdateGameTextAssetCallback;
        }

        private static async void AutoUpdateGameTextAssetCallback()
        {
            if (!Prefs.autoUpdate){ return; }

            if (EditorApplication.isCompiling) { return; }

            if (nextCheckTime.HasValue)
            {
                if (DateTime.Now < nextCheckTime) { return; }
            }

            nextCheckTime = DateTime.Now.AddSeconds(CheckInterval);

            var config = GameTextConfig.Instance;

            //------ Embedded ------

            if (embeddedAsset == null)
            {
                embeddedAsset = GameTextLoader.LoadGameTextAsset(ContentType.Embedded);
            }

            if (embeddedAsset != null)
            {
                await UpdateGameText(embeddedAsset, config.Embedded);
            }

            //------ Distribution ------

            if (distributionAsset == null)
            {
                distributionAsset = GameTextLoader.LoadGameTextAsset(ContentType.Distribution);
            }

            if (distributionAsset != null)
            {
                await UpdateGameText(distributionAsset, config.Distribution);
            }
        }

        private static async Task UpdateGameText(GameTextAsset gameTextAsset, GameTextConfig.GenerateAssetSetting setting)
        {
            if (gameTextAsset == null){ return; }

            var excelPath = setting.GetExcelPath();

            if (!File.Exists(excelPath)){ return; }

            var lastUpdate = File.GetLastWriteTime(excelPath).ToUnixTime();

            if (!gameTextAsset.UpdateAt.HasValue){ return; }
            
            if (lastUpdate < gameTextAsset.UpdateAt){ return; }

            var languageInfo = GameTextLanguage.GetCurrentInfo();

            await GameTxetExcel.Export(gameTextAsset.ContentType);

            GameTextGenerator.Generate(gameTextAsset.ContentType, languageInfo);

            UnityConsole.Info("GameText auto updated.");
        }
    }
}
