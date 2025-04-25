
using UnityEditor;
using Extensions;
using Extensions.Devkit;
using TMPro;

namespace Modules.Devkit.TextMeshPro
{
    public static class CleanDynamicFontAsset
    {
        //----- params -----

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

            if (focus)
            {
                foreach (var fontAsset in fontAssets)
                {
                    Reflection.InvokePrivateMethod(fontAsset, "UpdateFontAssetData");
                }
            }
            else
            {
                foreach (var fontAsset in fontAssets)
                {
                    fontAsset.ClearFontAssetData(true);

                    AssetDatabase.SaveAssetIfDirty(fontAsset);
                }
            }
        }
    }
}