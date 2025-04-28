
using System;
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
            public ContentsInfo contentsInfo = null;
            public int textIndex = 0;
            public bool generateScript = true;
            public bool force = false;
        }

        public sealed class ContentsInfo
        {
            public string hash { get; set; } = null;

            public string[] index { get; set; }

            public List<SheetData> sheetDatas { get; set; }
        }

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Generate(TextType type, LanguageInfo info, bool force = false)
        {
            if (info == null){ return; }

            var textData = TextData.Instance;

            var config = TextDataConfig.Instance;

            TextDataSource[] sources = null;

            var scriptFolderPath = string.Empty;

            var assetFolderPath = string.Empty;

            switch (type)
            {
                case TextType.Internal:
                    {
                        var internalSettings = config.Internal;

                        sources = internalSettings.Source;
                        scriptFolderPath = internalSettings.ScriptFolderPath;
                        assetFolderPath = internalSettings.AseetFolderPath;
                    }
                    break;

                case TextType.External:
                    {
                        if (!config.EnableExternal) { return; }

                        var externalSettings = config.External;

                        sources = externalSettings.Source;
                        assetFolderPath = externalSettings.AseetFolderPath;
                    }
                    break;
            }

            var assetFolderLocalPath = textData.AssetFolderLocalPath;

            var assetFileName = TextData.GetAssetFileName(info.Identifier);

            var assetPath = PathUtility.Combine(assetFolderPath, assetFolderLocalPath, assetFileName);

            var contentsInfo = BuildContentsInfo(sources);

            if (contentsInfo == null) { return; }

            var generateInfo = new GenerateInfo
            {
                assetPath = assetPath,
                scriptFolderPath = scriptFolderPath,
                contentsInfo = contentsInfo,
                textIndex = info.TextIndex,
                generateScript = info.ScriptGenerate,
                force = force,
            };

            GenerateTextData(type, generateInfo);
        }

        private static ContentsInfo BuildContentsInfo(TextDataSource[] sources)
        {
            var progressTitle = "Build TextData Contents";
            
            var config = TextDataConfig.Instance;

            var contentsInfo = new ContentsInfo();

            try
            {
                //----- 読み込み -----

                var sheetDatas = new List<SheetData>();

                foreach (var source in sources)
                {
                    EditorUtility.DisplayProgressBar(progressTitle, "Load contents.", 0f);

                    var contentsFolderPath = source.GetContentsFolderPath();

                    var index = LoadIndexData(config.FileFormat, contentsFolderPath);

                    var sheets = LoadSheetData(config.FileFormat, contentsFolderPath);

                    if (sheets == null) { continue; }

                    var sheetIndex = sheetDatas.Any() ? sheetDatas.Max(x => x.index) + 1 : 0;

                    foreach (var item in sheets)
                    {
                        item.index = sheetIndex + index.sheetNames.IndexOf(x => x == item.displayName);
                    }

                    foreach (var item in sheets)
                    {
                        var sheetData = sheetDatas.FirstOrDefault(x => x.guid == item.guid);

                        if (sheetData == null)
                        {
                            sheetDatas.Add(item);
                        }
                        else
                        {
                            var hashSource = sheetData.hash + item.hash;

                            sheetData.hash = hashSource.GetHash();

                            sheetData.records.AddRange(item.records);
                        }
                    }
                }

                contentsInfo.sheetDatas = sheetDatas;

                //----- 同じテキストGUIDが存在しないか検証 -----

                var hasError = false;

                var logBuilder = new StringBuilder();

                var allRecords = new List<Tuple<SheetData, RecordData>>();

                foreach (var item in contentsInfo.sheetDatas)
                {
                    foreach (var record in item.records)
                    {
                        allRecords.Add(Tuple.Create(item, record));
                    }
                }

                var groups = allRecords.GroupBy(x => x.Item2.guid).ToArray();

                foreach (var group in groups)
                {
                    logBuilder.Clear();

                    var duplication = 1 < group.Count();

                    if (duplication)
                    {
                        logBuilder.AppendLine($"Contents Duplication Text Guid : {group.Key}");
                        logBuilder.AppendLine();

                        foreach (var item in group)
                        {
                            logBuilder.AppendLine($"SheetName : {item.Item1.sheetName}, EnumName : {item.Item2.enumName}");
                        }

                        Debug.LogError(logBuilder.ToString());

                        hasError = true;
                    }
                }

                if (hasError) { return null; }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            contentsInfo.hash = CreateSheetsHash(contentsInfo.sheetDatas);

            return contentsInfo;
        }

        private static void GenerateTextData(TextType type, GenerateInfo generateInfo)
        {
            var progressTitle = "Generate TextData";
            
            var config = TextDataConfig.Instance;

            var cryptoKey = new AesCryptoKey(config.CryptoKey, config.CryptoIv);

            var generateScript = !string.IsNullOrEmpty(generateInfo.scriptFolderPath) && generateInfo.generateScript;

            var textDataAsset = LoadAsset(generateInfo.assetPath);

            var contentsInfo = generateInfo.contentsInfo;

            if (!generateInfo.force)
            {
                // 中身のデータに変化がないので更新しない.
                if (textDataAsset != null && textDataAsset.Hash == contentsInfo.hash) { return; }
            }

            try
            {
				using (new AssetEditingScope())
                {
                    var hash = contentsInfo.hash;
					var sheets = contentsInfo.sheetDatas.OrderBy(x => x.index).ToArray();

					// Asset.

					EditorUtility.DisplayProgressBar(progressTitle, "Generate asset.", 0.5f);

					TextDataAssetGenerator.Build(textDataAsset, type, sheets, hash, generateInfo.textIndex, cryptoKey);

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

		private static string CreateSheetsHash(IEnumerable<SheetData> sheetDatas)
		{
			var builder = new StringBuilder();

			var items = sheetDatas.OrderBy(x => x.guid);

			foreach (var item in items)
			{
				builder.AppendLine(item.hash);
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

            var indexFile = Directory.EnumerateFiles(recordDirectory, "*.*", SearchOption.AllDirectories)
                .Where(x => Path.GetExtension(x) == IndexFileExtension)
                .Select(x => PathUtility.ConvertPathSeparator(x))
                .FirstOrDefault();

            var indexData = FileLoader.LoadFile<IndexData>(indexFile, fileFormat);

            return indexData;
        }

        private static List<SheetData> LoadSheetData(FileLoader.Format fileFormat, string recordDirectory)
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

				var hash = FileUtility.GetHash(sheetFile);

                if (sheetData != null)
                {
                    sheetData.hash = hash;

                    list.Add(sheetData);
                }
            }

            return list;
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
