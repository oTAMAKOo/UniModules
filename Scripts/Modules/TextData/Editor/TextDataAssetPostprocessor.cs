﻿
using UnityEngine;
using UnityEditor;
using Modules.TextData.Components;

namespace Modules.TextData.Editor
{
    public sealed class TextDataAssetPostprocessor : AssetPostprocessor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public override int GetPostprocessOrder()
        {
            return 100;
        }

        /// <summary>
        /// あらゆる種類の任意の数のアセットがインポートが完了したときに呼ばれる処理です。
        /// </summary>
        /// <param name="importedAssets"> インポートされたアセットのファイルパス。 </param>
        /// <param name="deletedAssets"> 削除されたアセットのファイルパス。 </param>
        /// <param name="movedAssets"> 移動されたアセットのファイルパス。 </param>
        /// <param name="movedFromPath"> 移動されたアセットの移動前のファイルパス。 </param>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath)
        {
			if (Application.isBatchMode){ return; }

            foreach (var importedAsset in importedAssets)
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(importedAsset);

                if (asset is TextDataAsset)
                {
                    TextDataLoader.Reload();
                    break;
                }
            }
        }
    }
}
