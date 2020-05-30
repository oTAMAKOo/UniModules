
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

        public sealed class GenerateInfo
        {
            public string assetPath = null;
            public string scriptFolderPath = null;
            public string contentsFolderPath = null;
            public int textIndex = 0;
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Generate(GenerateInfo info)
        {
            var progressTitle = "Generate GameText";

            var gameText = GameText.Instance;

            var config = GameTextConfig.Instance;

            // 読み込み.

            EditorUtility.DisplayProgressBar(progressTitle, "Load contents.", 0f);

            var sheets = LoadSheetData(config.FileFormat, info.contentsFolderPath);

            if (sheets == null) { return; }

            var aesManaged = gameText.GetAesManaged();

            var generateScript = !string.IsNullOrEmpty(info.scriptFolderPath);

            try
            {
                AssetDatabase.StartAssetEditing();

                // Script.

                if (generateScript)
                {
                    EditorApplication.LockReloadAssemblies();

                    DirectoryUtility.Clean(info.scriptFolderPath);

                    AssetDatabase.ImportAsset(info.scriptFolderPath, ImportAssetOptions.ForceUpdate);

                    EditorUtility.DisplayProgressBar(progressTitle, "Generate script.", 0.25f);

                    CategoryScriptGenerator.Generate(sheets, info.scriptFolderPath);

                    GameTextScriptGenerator.Generate(sheets, info.scriptFolderPath);

                    ContentsScriptGenerator.Generate(sheets, info.scriptFolderPath, info.textIndex);
                }

                // Asset.

                EditorUtility.DisplayProgressBar(progressTitle, "Generate asset.", 0.5f);
                
                var gameTextAsset = LoadAsset(info.assetPath);

                GameTextAssetGenerator.Build(gameTextAsset, sheets, info.textIndex, aesManaged);

                EditorUtility.DisplayProgressBar(progressTitle, "Complete.", 1f);

                UnityConsole.Info("GameTextを出力しました");

                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (generateScript)
                {
                    EditorApplication.UnlockReloadAssemblies();
                }

                AssetDatabase.StopAssetEditing();

                AssetDatabase.Refresh();

                EditorUtility.ClearProgressBar();
            }

            EditorUtility.ClearProgressBar();
        }

        private static SheetData[] LoadSheetData(FileLoader.Format fileFormat, string recordDirectory)
        {
            var extension = FileLoader.GetFileExtension(fileFormat);

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
                var sheetData = FileLoader.LoadFile<SheetData>(sheetFile, fileFormat);

                if (sheetData != null)
                {
                    list.Add(sheetData);
                }
            }

            return list.OrderBy(x => x.index).ToArray();
        }

        private static GameTextAsset LoadAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) { return null; }

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath) as GameTextAsset;

            if (asset == null)
            {
                asset = ScriptableObjectGenerator.Generate<GameTextAsset>(assetPath);
            }

            return asset;
        }
    }
}
