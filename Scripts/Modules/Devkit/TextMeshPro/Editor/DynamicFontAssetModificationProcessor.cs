
using UnityEditor;
using System.IO;
using TMPro;
using Extensions.Devkit;

namespace Modules.Devkit.TextMeshPro
{
	public class DynamicFontAssetModificationProcessor : UnityEditor.AssetModificationProcessor
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
					
					fontAsset.ClearFontAssetData(true);
				}
			}

			return paths;
		}
	}
} 