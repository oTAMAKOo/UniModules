
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Modules.TextData.Components;

namespace Modules.TextData.Editor
{
    public sealed class TextDataAssetGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Build(TextDataAsset asset, ContentType contentType, SheetData[] sheets, string hash, int textIndex, AesCryptoKey aesCryptoKey)
        {
            var categoryContents = new List<TextDataAsset.CategoryContent>();

            for (var i = 0; i < sheets.Length; i++)
            {
                var records = sheets[i].records;

                if (string.IsNullOrEmpty(sheets[i].sheetName)){ continue; }

                var textContents = new List<TextDataAsset.TextContent>();

                for (var j = 0; j < records.Length; j++)
                {
                    var record = records[j];

                    var guid = TextDataGuid.Get(record.identifier);

                    var cryptIdentifier = record.identifier.Encrypt(aesCryptoKey);

                    var enumName = record.enumName.Encrypt(aesCryptoKey);
                    
                    var text = record.texts.ElementAtOrDefault(textIndex);

                    var cryptText = string.IsNullOrEmpty(text) ? string.Empty : text.Encrypt(aesCryptoKey);

                    var textContent = new TextDataAsset.TextContent(cryptIdentifier, guid, enumName, cryptText);

                    textContents.Add(textContent);
                }

                var sheetGuid = TextDataGuid.Get(sheets[i].sheetName);
                var cryptSheetName = sheets[i].sheetName.Encrypt(aesCryptoKey);
                var cryptSheetDisplayName = sheets[i].displayName.Encrypt(aesCryptoKey);

                var sheetContent = new TextDataAsset.CategoryContent(sheetGuid, cryptSheetName, cryptSheetDisplayName, textContents.ToArray());

                categoryContents.Add(sheetContent);
            }
			
            asset.SetContents(contentType, hash, categoryContents.ToArray());

            EditorUtility.SetDirty(asset);
        }
    }
}
