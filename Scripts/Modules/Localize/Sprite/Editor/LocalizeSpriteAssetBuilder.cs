
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.Localize.Editor
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
				SetFolderInfo(target, cryptoKey);

				SetSpriteFolderInfo(target);

				UnityEditorUtility.SaveAsset(target);
			}

			using (new DisableStackTraceScope())
			{
				Debug.Log("Update LocalizeAsset complete.");
			}
		}

		private static void SetFolderInfo(LocalizeSpriteAsset target, AesCryptoKey cryptoKey)
		{
			var folderDictionary = new LocalizeSpriteAsset.FolderDictionary();

			var folderInfos = target.Infos.ToArray();

			foreach (var folderInfo in folderInfos)
			{
				if (string.IsNullOrEmpty(folderInfo.guid)){ continue; }

				var folderPath = AssetDatabase.GUIDToAssetPath(folderInfo.guid);

				var encrypted = folderPath.Encrypt(cryptoKey);
				
				folderDictionary.Add(folderInfo.guid, encrypted);
			}

			Reflection.SetPrivateField(target, "folderDictionary", folderDictionary);
		}

		private static void SetSpriteFolderInfo(LocalizeSpriteAsset target)
		{
			var spriteDictionary = new LocalizeSpriteAsset.SpriteDictionary();

			var folderInfos = target.Infos.ToArray();

			foreach (var folderInfo in folderInfos)
			{
				if (string.IsNullOrEmpty(folderInfo.guid)){ continue; }

				var folderPath = AssetDatabase.GUIDToAssetPath(folderInfo.guid);

				if (string.IsNullOrEmpty(folderPath)){ continue; }

				var spriteGuids = AssetDatabase.FindAssets("t:" + nameof(Sprite), new string[] { folderPath });

				foreach (var spriteGuid in spriteGuids)
				{
					spriteDictionary.Add(spriteGuid, folderInfo.guid);
				}
			}

			Reflection.SetPrivateField(target, "spriteDictionary", spriteDictionary);
		}
	}
}