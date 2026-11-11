
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.TextMeshPro
{
    public sealed class DynamicFontAssetModificationProcessor : UnityEditor.AssetModificationProcessor
    {
        private const string FontAssetExtension = ".asset";

        private static HashSet<TMP_FontAsset> waitForProcess = new HashSet<TMP_FontAsset>();

        static string[] OnWillSaveAssets(string[] paths)
        {
            foreach (var path in paths)
            {
                var extension = Path.GetExtension(path);

                if (extension != FontAssetExtension){ continue; }

                var fontAsset = AssetDatabase.LoadAssetAtPath(path, typeof(TMP_FontAsset)) as TMP_FontAsset;

                if (fontAsset == null){ continue; }
				
                if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic){ continue; }
					
                DelayCleanTMPFontAsset(fontAsset).Forget();
            }

            return paths;
        }

        private static async UniTask DelayCleanTMPFontAsset(TMP_FontAsset fontAsset)
        {
            // 処理中の多重実行防止（ClearFontAssetData後のSaveAssetIfDirtyで再入するため）.
            if (waitForProcess.Contains(fontAsset)){ return; }

            waitForProcess.Add(fontAsset);

            // 早期returnでもwaitForProcessから必ず除外する.
            // 除外漏れがあると以降そのフォントはContainsで弾かれ続け、クリーンアップが二度と動かなくなる.
            try
            {
                if (fontAsset.glyphTable.IsEmpty()){ return; }

                if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic){ return; }

                await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.Realtime);

                using (new AssetEditingScope())
                {
                    fontAsset.ClearFontAssetData(true);

                    AssetDatabase.SaveAssetIfDirty(fontAsset);
                }
            }
            finally
            {
                waitForProcess.Remove(fontAsset);
            }
        }
    }
} 