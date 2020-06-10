
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Extensions;
using Modules.GameText.Components;

namespace Modules.GameText.Editor
{
    public sealed class GameTextAssetGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Build(GameTextAsset asset, SheetData[] sheets, int textIndex, AesManaged aesManaged)
        {
            var contents = new List<TextContent>();

            for (var i = 0; i < sheets.Length; i++)
            {
                var records = sheets[i].records;

                for (var j = 0; j < records.Length; j++)
                {
                    var record = records[j];

                    var contentData = record.contents.ElementAtOrDefault(textIndex);

                    var text = contentData != null ? contentData.text.Encrypt(aesManaged) : string.Empty;

                    var textContent = new TextContent(record.guid, text);

                    contents.Add(textContent);
                }
            }
            
            asset.SetContents(contents.ToArray(), DateTime.Now.ToUnixTime());

            EditorUtility.SetDirty(asset);
        }
    }
}
