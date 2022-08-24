
using UnityEngine;
using Extensions;
using Extensions.Devkit;

namespace Modules.Localize
{
    public static class LocalizeSpriteAssetBuilder
    {
        //----- params -----

		//----- field -----

        //----- property -----

        //----- method -----
		
		public static void Build()
		{
			var config = LocalizeSpriteConfig.Instance;

			var cryptoKey = config.GetCryptoKey();

			var targets = UnityEditorUtility.FindAssetsByType<LocalizeSpriteAsset>($"t:{ typeof(LocalizeSpriteAsset).FullName }");

			foreach (var target in targets)
			{
				LocalizeSpriteAssetUpdater.SetFolderInfo(target, cryptoKey);

				LocalizeSpriteAssetUpdater.SetSpriteFolderInfo(target);

				UnityEditorUtility.SaveAsset(target);
			}

			using (new DisableStackTraceScope())
			{
				Debug.Log("Update LocalizeAsset complete.");
			}
		}
	}
}