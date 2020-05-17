
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Modules.Devkit;
using Modules.Devkit.Generators;
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

            // 読み込み.

            EditorUtility.DisplayProgressBar(progressTitle, "Load contents.", 0f);

            var sheets = LoadSheetData(config);

            if (sheets == null) { return; }

            if (sheets.Any(x => x.records != null && x.records.Any()))
            {
                AssetDatabase.StartAssetEditing();

                try
                {
                    EditorUtility.DisplayProgressBar(progressTitle, "Generate script.", 0.25f);

                    CategoryScriptGenerator.Generate(sheets, config);

                    ContentsScriptGenerator.Generate(sheets, config, languageInfo.TextIndex);

                    GameTextScriptGenerator.Generate(sheets, config);

                    EditorUtility.DisplayProgressBar(progressTitle, "Generate asset.", 0.5f);

                    foreach (var assetFolderPath in config.AssetFolderPaths)
                    {
                        var gameTextAsset = LoadAsset(assetFolderPath, languageInfo.AssetName);

                        GameTextAssetGenerator.Build(gameTextAsset, sheets, config, languageInfo.TextIndex);
                    }

                    EditorUtility.DisplayProgressBar(progressTitle, "Complete.", 1f);

                    UnityConsole.Info("GameTextを出力しました");
                    
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
            var recordDirectory = config.GetContentsFolderPath();

            var extension = config.GetFileExtension();

            if (!Directory.Exists(recordDirectory))
            {
                Debug.LogErrorFormat("Directory {0} not found.", recordDirectory);
                return null;
            }

            var sheetFiles = Directory.EnumerateFiles(recordDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(x => Path.GetExtension(x) == extension)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .ToArray();

            var list = new List<SheetData>();

            foreach (var sheetFile in sheetFiles)
            {
                var sheetData = FileLoader.LoadFile<SheetData>(sheetFile, config.FileFormat);

                if (sheetData != null)
                {
                    list.Add(sheetData);
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
    }
}
