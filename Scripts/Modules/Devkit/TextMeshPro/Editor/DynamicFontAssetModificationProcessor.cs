
using UnityEditor;
using System.IO;
using TMPro;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.TextMeshPro
{
	public sealed class DynamicFontAssetModificationProcessor : UnityEditor.AssetModificationProcessor
	{
		private const string FontAssetExtension = ".asset";

		static string[] OnWillSaveAssets(string[] paths)
		{
			using (new AssetEditingScope())
			{
				foreach (var path in paths)
				{
					var extension = Path.GetExtension(path);

					if (extension != FontAssetExtension){ continue; }

					var fontAsset = AssetDatabase.LoadAssetAtPath(path, typeof(TMP_FontAsset)) as TMP_FontAsset;

					if (fontAsset == null){ continue; }
				
					if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic){ continue; }
					
                    DelayCleanTMPFontAsset(fontAsset);
				}
			}

			return paths;
		}

        private static void DelayCleanTMPFontAsset(TMP_FontAsset fontAsset)
        {
            if (fontAsset.glyphTable.IsEmpty()){ return; }

            EditorApplication.delayCall += () =>
            {
                fontAsset.ClearFontAssetData(true);

                AssetDatabase.SaveAssetIfDirty(fontAsset);
            };
        }
	}
} 