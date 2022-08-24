
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
                var sheetGuid = sheets[i].guid;
                var sheetName = sheets[i].sheetName.Encrypt(aesCryptoKey);
                var sheetDisplayName = sheets[i].displayName.Encrypt(aesCryptoKey);
                var records = sheets[i].records;

                if (string.IsNullOrEmpty(sheetName)){ continue; }

                var textContents = new List<TextDataAsset.TextContent>();

                for (var j = 0; j < records.Length; j++)
                {
                    var record = records[j];

                    var text = record.texts.ElementAtOrDefault(textIndex);

                    var enumName = record.enumName.Encrypt(aesCryptoKey);
                    
                    var cryptText = string.IsNullOrEmpty(text) ? string.Empty : text.Encrypt(aesCryptoKey);

                    var textContent = new TextDataAsset.TextContent(record.guid, enumName, cryptText);

                    textContents.Add(textContent);
                }

                var sheetContent = new TextDataAsset.CategoryContent(sheetGuid, sheetName, sheetDisplayName, textContents.ToArray());

                categoryContents.Add(sheetContent);
            }
			
            asset.SetContents(contentType, hash, categoryContents.ToArray());

            EditorUtility.SetDirty(asset);
        }
    }
}
