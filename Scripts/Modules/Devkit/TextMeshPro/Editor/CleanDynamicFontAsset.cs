
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
                fontAssets.ForEach(x => Reflection.InvokePrivateMethod(x, "UpdateFontAssetData"));
            }
            else
            {
                fontAssets.ForEach(x => x.ClearFontAssetData(true));
            }
        }
    }
}