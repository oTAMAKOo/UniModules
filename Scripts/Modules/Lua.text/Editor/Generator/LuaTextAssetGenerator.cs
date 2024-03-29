﻿
#if ENABLE_XLUA

using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Generators;

namespace Modules.Lua.Text
{
    public static class LuaTextAssetGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		public static bool IsRequireUpdate(BookData bookData)
		{
			var language = LuaTextLanguage.Instance.Current;

			var assetPath = LuaText.GetAssetFileName(bookData.DestPath, language.Identifier);

			var asset = AssetDatabase.LoadAssetAtPath<LuaTextAsset>(assetPath);

			if (asset == null){ return true; }

			return asset.Hash != bookData.hash;
		}

		public static void Generate(BookData bookData, SheetData[] sheetDatas)
		{
			var config = LuaTextConfig.Instance;

			var language = LuaTextLanguage.Instance.Current;

			var aesCryptoKey = config.GetCryptoKey();

			if (aesCryptoKey == null){ return; }

			var assetPath = LuaText.GetAssetFileName(bookData.DestPath, language.Identifier);

			var asset = AssetDatabase.LoadAssetAtPath<LuaTextAsset>(assetPath);

			if (asset == null)
			{
				asset = ScriptableObjectGenerator.Generate<LuaTextAsset>(assetPath, false);
			}

			var list = new List<LuaTextAsset.Content>();

			foreach (var sheet in sheetDatas)
			{
				var sheetName = sheet.sheetName.Encrypt(aesCryptoKey);
				var summary = sheet.summary.Encrypt(aesCryptoKey);

				var content = new LuaTextAsset.Content()
				{
					sheetName = sheetName,
					summary = summary,
				};

				var texts = new List<LuaTextAsset.TextData>();

				foreach (var record in sheet.records)
				{
					var text = record.texts.ElementAtOrDefault(language.TextIndex);

					var cryptText = text.Encrypt(aesCryptoKey);

					var textData = new LuaTextAsset.TextData(record.id, cryptText);

					texts.Add(textData);
				}

				content.texts = texts.ToArray();

				list.Add(content);
			}

			var fileName = bookData.bookName;
			var rootFolderGuid = AssetDatabase.AssetPathToGUID(bookData.destDirectory);
			var contents = list.ToArray();
			var hash = bookData.hash;
			
			asset.SetContents(fileName, rootFolderGuid, contents, hash);
			
			UnityEditorUtility.SaveAsset(asset);
		}
    }
}

#endif