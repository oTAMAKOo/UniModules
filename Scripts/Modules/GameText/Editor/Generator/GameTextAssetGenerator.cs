﻿﻿
using System.Linq;
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

        public static void Build(GameTextAsset asset, RecordData[] records, GameTextConfig config, int textColumn)
        {
            var aesManaged = AESExtension.CreateAesManaged(GameText.AESKey, GameText.AESIv);

            var gameTextDictionarys = new List<GameText.GameTextDictionary>();

            var recordGroupBySheet = records.GroupBy(x => x.sheet);

            foreach (var sheetRecords in recordGroupBySheet)
            {
                var gameTextDictionary = new GameText.GameTextDictionary();
                
                foreach (var sheetRecord in sheetRecords)
                {
                    var key = sheetRecord.guid;
                    var value = sheetRecord.texts[textColumn].Encrypt(aesManaged);

                    gameTextDictionary.Add(key, value);
                }

                gameTextDictionarys.Add(gameTextDictionary);
            }

            asset.contents = gameTextDictionarys.ToArray();
        }
    }
}
