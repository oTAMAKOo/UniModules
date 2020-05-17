
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Modules.GameText.Components;

namespace Modules.GameText.Editor
{
    public class GameTextAssetGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Build(GameTextAsset asset, SheetData[] sheets, GameTextConfig config, int textIndex)
        {
            var aesManaged = AESExtension.CreateAesManaged(GameText.AESKey, GameText.AESIv);

            var contents = new List<TextContent>();

            for (var i = 0; i < sheets.Length; i++)
            {
                var records = sheets[i].records;

                for (var j = 0; j < records.Length; j++)
                {
                    var record = records[j];

                    var contentData = record.contents.ElementAtOrDefault(textIndex);

                    var text = contentData != null ? contentData.text.Encrypt(aesManaged) : string.Empty;

                    var textContent = new TextContent(record.guid, text, i);

                    contents.Add(textContent);
                }
            }
            
            asset.SetContents(contents.ToArray());

            EditorUtility.SetDirty(asset);
        }
    }
}
