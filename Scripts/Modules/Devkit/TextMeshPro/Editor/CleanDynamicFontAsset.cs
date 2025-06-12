
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

            var targetFontAssets = fontAssets.Where(x => TargetModeTable.Contains(x.atlasPopulationMode)).ToArray();

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