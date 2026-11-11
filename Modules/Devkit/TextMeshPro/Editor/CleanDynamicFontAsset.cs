
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using TMPro;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.TextMeshPro
{
    public static class CleanDynamicFontAsset
    {
        //----- params -----

        public static class Prefs
        {
            public static bool disable
            {
                get { return ProjectPrefs.Get(typeof(Prefs).FullName + "-disable", false); }
                set { ProjectPrefs.Set(typeof(Prefs).FullName + "-disable", value); }
            }
        }

        private static readonly HashSet<AtlasPopulationMode> TargetModeTable = new HashSet<AtlasPopulationMode>
        {
            AtlasPopulationMode.Dynamic, 
            AtlasPopulationMode.DynamicOS,
        };

        //----- field -----

        //----- property -----

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            EditorApplication.focusChanged += OnFocusChanged;
        }

        private static void OnFocusChanged(bool focus)
        {
            if (Application.isPlaying){ return; }

            if (Prefs.disable){ return; }

            var fontAssets = UnityEditorUtility.FindAssetsByType<TMP_FontAsset>($"t:{typeof(TMP_FontAsset).FullName}");

            // グリフ未生成のフォントは対象外.
            // TMP_FontAsset.ClearFontAssetData は中身が空でも無条件にアセットをDirty化して保存させるため、
            // 除外しないとフォーカス切替の度に不要な再インポートが発生する.
            var targetFontAssets = fontAssets
                .Where(x => TargetModeTable.Contains(x.atlasPopulationMode))
                .Where(x => !x.glyphTable.IsEmpty())
                .ToArray();

            // 対象が無い場合はAssetEditingScopeに入らない（StopAssetEditingでRefreshが走るのを避ける）.
            if (targetFontAssets.IsEmpty()){ return; }

            using (new AssetEditingScope())
            {
                if (focus)
                {
                    foreach (var fontAsset in targetFontAssets)
                    {
                        Reflection.InvokePrivateMethod(fontAsset, "UpdateFontAssetData");
                    }
                }
                else
                {
                    foreach (var fontAsset in targetFontAssets)
                    {
                        fontAsset.ClearFontAssetData(true);

                        AssetDatabase.SaveAssetIfDirty(fontAsset);
                    }
                }
            }
        }
    }
}