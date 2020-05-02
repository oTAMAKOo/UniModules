
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Modules.Devkit;
using Modules.Devkit.Generators;
using Modules.Devkit.Spreadsheet;
using Modules.GameText.Components;

namespace Modules.GameText.Editor
{
    public static class GameTextGenerater
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Generate(GameTextLanguage.Info languageInfo)
        {
            if (languageInfo == null) { return; }

            var progressTitle = "Generate GameText";
           
            var config = GameTextConfig.Instance;

            var gameTextAsset = LoadAsset(config.ScriptableObjectFolderPath, languageInfo.AssetPath);

            // 読み込み.

            EditorUtility.DisplayProgressBar(progressTitle, "Load contents.", 0f);

            var sheets = LoadSheetData(config);

            if (sheets == null) { return; }

            var records = LoadRecordData(config);

            if (records == null) { return; }

            if (records.Any())
            {
                AssetDatabase.StartAssetEditing();

                try
                {
                    EditorUtility.DisplayProgressBar(progressTitle, "Generate script.", 0.25f);

                    CategoryScriptGenerator.Generate(sheets, config);

                    ContentsScriptGenerator.Generate(sheets, records, config, languageInfo.TextIndex);

                    GameTextScriptGenerator.Generate(sheets, config);

                    EditorUtility.DisplayProgressBar(progressTitle, "Generate asset.", 0.5f);

                    GameTextAssetGenerator.Build(gameTextAsset, records, config, languageInfo.TextIndex);

                    EditorUtility.DisplayProgressBar(progressTitle, "Complete.", 1f);

                    UnityConsole.Info("GameTextを出力しました");

                    SaveLastUpdateDate(gameTextAsset, DateTime.Now.ToUnixTime());

                    AssetDatabase.SaveAssets();
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();

                    EditorUtility.ClearProgressBar();
                }
            }
            else
            {
                Debug.Log("GameText record not found.");
            }

            EditorUtility.ClearProgressBar();
        }

        private static SheetData[] LoadSheetData(GameTextConfig config)
        {
            var recordDirectory = config.GetRecordFolderPath();

            var sheetFileExtension = config.GetSheetFileExtension();

            if (!Directory.Exists(recordDirectory))
            {
                Debug.LogErrorFormat("Directory {0} not found.", recordDirectory);
                return null;
            }

            var sheetFiles = Directory.EnumerateFiles(recordDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(x => Path.GetExtension(x) == sheetFileExtension)
                .ToArray();

            var list = new List<SheetData>();

            foreach (var sheetFile in sheetFiles)
            {
                var sheetData = FileSystem.LoadFile<SheetData>(sheetFile, config.FileFormat);

                if (sheetData != null)
                {
                    list.Add(sheetData);
                }
            }

            return list.ToArray();
        }

        private static RecordData[] LoadRecordData(GameTextConfig config)
        {
            var recordDirectory = config.GetRecordFolderPath();

            var recordFileExtension = config.GetRecordFileExtension();

            if (!Directory.Exists(recordDirectory))
            {
                Debug.LogErrorFormat("Directory {0} not found.", recordDirectory);
                return null;
            }

            var recordFiles = Directory.EnumerateFiles(recordDirectory, "*.*", SearchOption.AllDirectories)
                .Where(x => Path.GetExtension(x) == recordFileExtension)
                .ToArray();

            var list = new List<RecordData>();

            foreach (var recordFile in recordFiles)
            {
                var recordData = FileSystem.LoadFile<RecordData>(recordFile, config.FileFormat);

                if (recordData != null)
                {
                    list.Add(recordData);
                }
            }

            return list.ToArray();
        }

        private static GameTextAsset LoadAsset(string assetFolderPath, string resourcesPath)
        {
            var assetPath = PathUtility.Combine(assetFolderPath, resourcesPath);

            if (string.IsNullOrEmpty(assetPath)) { return null; }

            var asset = AssetDatabase.LoadAssetAtPath<GameTextAsset>(assetPath);

            if (asset == null)
            {
                asset = ScriptableObjectGenerator.Generate<GameTextAsset>(assetPath);
            }

            return asset;
        }

        private static void SaveLastUpdateDate(GameTextAsset asset, long lastUpdateDate)
        {
            asset.updateTime = lastUpdateDate;
            EditorUtility.SetDirty(asset);
        }
    }
}
