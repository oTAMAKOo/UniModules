
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Generators;
using Modules.GameText.Components;

using DirectoryUtility = Extensions.DirectoryUtility;

namespace Modules.GameText.Editor
{
    public static class GameTextGenerator
    {
        //----- params -----

        private sealed class GenerateInfo
        {
            public string assetPath = null;
            public string scriptFolderPath = null;
            public string contentsFolderPath = null;
            public int textIndex = 0;
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Generate(ContentType type, GameTextLanguage.Info info)
        {
            var gameText = GameText.Instance;

            var config = GameTextConfig.Instance;

            var scriptFolderPath = string.Empty;

            var contentsFolderPath = string.Empty;

            var assetFolderPath = string.Empty;

            switch (type)
            {
                case ContentType.Embedded:
                    {
                        var embedded = config.Embedded;

                        scriptFolderPath = embedded.ScriptFolderPath;
                        contentsFolderPath = embedded.GetContentsFolderPath();
                        assetFolderPath = embedded.AseetFolderPath;
                    }
                    break;

                case ContentType.Distribution:
                    {
                        var distribution = config.Distribution;

                        if (!distribution.Enable) { return; }

                        contentsFolderPath = distribution.GetContentsFolderPath();
                        assetFolderPath = distribution.AseetFolderPath;
                    }
                    break;
            }

            var assetFolderName = gameText.GetAssetFolderName();

            var assetFileName = GameText.GetAssetFileName(info.Identifier);

            var assetPath = PathUtility.Combine(new string[] { assetFolderPath, assetFolderName, assetFileName });

            var generateInfo = new GenerateInfo
            {
                assetPath = assetPath,
                contentsFolderPath = contentsFolderPath,
                scriptFolderPath = scriptFolderPath,
                textIndex = info.TextIndex,
            };

            GenerateGameText(type, generateInfo);
        }
        
        private static void GenerateGameText(ContentType contentType, GenerateInfo generateInfo)
        {
            var progressTitle = "Generate GameText";

            var gameText = GameText.Instance;

            var config = GameTextConfig.Instance;

            // 読み込み.

            EditorUtility.DisplayProgressBar(progressTitle, "Load contents.", 0f);

            var sheets = LoadSheetData(config.FileFormat, generateInfo.contentsFolderPath);

            if (sheets == null) { return; }

            var cryptoKey = gameText.GetCryptoKey();

            var generateScript = !string.IsNullOrEmpty(generateInfo.scriptFolderPath);

            try
            {
                using (new AssetEditingScope())
                {
                    // Script.

                    if (generateScript)
                    {
                        EditorApplication.LockReloadAssemblies();

                        DirectoryUtility.Clean(generateInfo.scriptFolderPath);

                        AssetDatabase.ImportAsset(generateInfo.scriptFolderPath, ImportAssetOptions.ForceUpdate);

                        EditorUtility.DisplayProgressBar(progressTitle, "Generate script.", 0.25f);

                        CategoryScriptGenerator.Generate(sheets, generateInfo.scriptFolderPath);

                        GameTextScriptGenerator.Generate(sheets, generateInfo.scriptFolderPath);

                        ContentsScriptGenerator.Generate(sheets, generateInfo.scriptFolderPath, generateInfo.textIndex);
                    }

                    // Asset.

                    EditorUtility.DisplayProgressBar(progressTitle, "Generate asset.", 0.5f);

                    var gameTextAsset = LoadAsset(generateInfo.assetPath);

                    GameTextAssetGenerator.Build(gameTextAsset, contentType, sheets, generateInfo.textIndex, cryptoKey);

                    EditorUtility.DisplayProgressBar(progressTitle, "Complete.", 1f);
                }

                AssetDatabase.SaveAssets();
            }
            finally
            {
                if (generateScript)
                {
                    EditorApplication.UnlockReloadAssemblies();
                }

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
