
using UnityEditor;
using System;
using System.Collections.Generic;
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

        public static void Build(GameTextAsset asset, RecordData[] records, GameTextConfig config, int textIndex)
        {
            var aesManaged = AESExtension.CreateAesManaged(GameText.AESKey, GameText.AESIv);

            var contents = new List<TextContent>();
            
            foreach (var record in records)
            {
                var text = record.texts[textIndex].Encrypt(aesManaged);

                var textContent = new TextContent(record.guid, text, record.line);
                
                contents.Add(textContent);
            }
            
            asset.SetContents(contents.ToArray());

            EditorUtility.SetDirty(asset);
        }
    }
}
