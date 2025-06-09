
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Extensions;
using Extensions.Devkit;
using TMPro;
using UnityEditor.Localization.Plugins.XLIFF.V12;

namespace Modules.Devkit.TextMeshPro
{
    public static class CleanDynamicFontAsset
    {
        //----- params -----

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