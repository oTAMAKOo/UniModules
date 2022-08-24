
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;

namespace Modules.Localize
{
    public static class LocalizeSpriteAssetUpdater
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		public static void SetFolderInfo(LocalizeSpriteAsset target, AesCryptoKey cryptoKey)
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

		public static void SetSpriteFolderInfo(LocalizeSpriteAsset target)
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