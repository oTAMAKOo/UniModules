
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Generators;
using Modules.TextData.Components;

namespace Modules.TextData.Editor
{
    public static class TextDataGenerator
    {
        //----- params -----

        private const string IndexFileExtension = ".index";

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

        public static void Generate(ContentType type, LanguageInfo info, bool force = false)
        {
            var textData = TextData.Instance;

            var config = TextDataConfig.Instance;

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

            var assetFolderLocalPath = textData.AssetFolderLocalPath;

            var assetFileName = TextData.GetAssetFileName(info.Identifier);

            var assetPath = PathUtility.Combine(new string[] { assetFolderPath, assetFolderLocalPath, assetFileName });

            var generateInfo = new GenerateInfo
            {
                assetPath = assetPath,
                contentsFolderPath = contentsFolderPath,
                scriptFolderPath = scriptFolderPath,
                textIndex = info.TextIndex,
            };

            GenerateTextData(type, generateInfo, force);
        }
        
        private static void GenerateTextData(ContentType contentType, GenerateInfo generateInfo, bool force)
        {
            var progressTitle = "Generate TextData";
            
            var config = TextDataConfig.Instance;

            // 読み込み.

            EditorUtility.DisplayProgressBar(progressTitle, "Load contents.", 0f);

            var indexData = LoadIndexData(config.FileFormat, generateInfo.contentsFolderPath);

            var sheetDatas = LoadSheetData(config.FileFormat, generateInfo.contentsFolderPath);

			EditorUtility.ClearProgressBar();

            if (sheetDatas == null) { return; }

            var displayNames = indexData != null ? indexData.sheetNames : new string[0];

            var cryptoKey = new AesCryptoKey(config.CryptoKey, config.CryptoIv);

            var generateScript = !string.IsNullOrEmpty(generateInfo.scriptFolderPath);

            var textDataAsset = LoadAsset(generateInfo.assetPath);

            var hash = CreateSheetsHash(displayNames, sheetDatas);

            if (!force)
            {
                // 中身のデータに変化がないので更新しない.
                if (textDataAsset != null && textDataAsset.Hash == hash) { return; }
            }

            try
            {
				using (new AssetEditingScope())
                {
					var sheets = sheetDatas.Keys.ToArray();

                    if (displayNames.Any())
                    {
                        sheets = sheetDatas.Keys.OrderBy(x => displayNames.IndexOf(y => y == x.displayName)).ToArray();
                    }

					// Asset.

					EditorUtility.DisplayProgressBar(progressTitle, "Generate asset.", 0.5f);

					TextDataAssetGenerator.Build(textDataAsset, contentType, sheets, hash, generateInfo.textIndex, cryptoKey);

                    // Script.

                    if (generateScript)
                    {
                        EditorApplication.LockReloadAssemblies();

                        EditorUtility.DisplayProgressBar(progressTitle, "Generate script.", 0.25f);

                        CategoryScriptGenerator.Generate(sheets, generateInfo.scriptFolderPath);

                        TextDataScriptGenerator.Generate(sheets, generateInfo.scriptFolderPath);

                        ContentsScriptGenerator.Generate(sheets, generateInfo.scriptFolderPath, generateInfo.textIndex);
                    }

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

		private static string CreateSheetsHash(string[] displayNames, Dictionary<SheetData, string> sheetDatas)
		{
			var builder = new StringBuilder();

			var items = sheetDatas.ToArray();

            if (displayNames.Any())
            {
                items = sheetDatas.OrderBy(x => displayNames.IndexOf(y => y == x.Key.displayName)).ToArray();
            }

			foreach (var item in items)
			{
				builder.AppendLine(item.Value);
			}

			return builder.ToString().GetHash();
		}

        private static IndexData LoadIndexData(FileLoader.Format fileFormat, string recordDirectory)
        {
            if (!Directory.Exists(recordDirectory))
            {
                Debug.LogErrorFormat("Directory {0} not found.", recordDirectory);
                return null;
            }

            var indexFile = Directory.EnumerateFiles(recordDirectory, "*.*", SearchOption.TopDirectoryOnly)
                .Where(x => Path.GetExtension(x) == IndexFileExtension)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .FirstOrDefault();

            var indexData = FileLoader.LoadFile<IndexData>(indexFile, fileFormat);

            return indexData;
        }

        private static Dictionary<SheetData, string> LoadSheetData(FileLoader.Format fileFormat, string recordDirectory)
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

            var dictionary = new Dictionary<SheetData, string>();

            foreach (var sheetFile in sheetFiles)
            {
                var sheetData = FileLoader.LoadFile<SheetData>(sheetFile, fileFormat);

				var hash = FileUtility.GetHash(sheetFile);

                if (sheetData != null)
                {
					dictionary.Add(sheetData, hash);
                }
            }

            return dictionary;
        }

        private static TextDataAsset LoadAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)) { return null; }

            var asset = AssetDatabase.LoadMainAssetAtPath(assetPath) as TextDataAsset;

            if (asset == null)
            {
                asset = ScriptableObjectGenerator.Generate<TextDataAsset>(assetPath);
            }

            return asset;
        }
    }
}
