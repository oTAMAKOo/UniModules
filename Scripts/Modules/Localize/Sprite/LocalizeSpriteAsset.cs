
using UnityEngine;
using System;
using System.Collections.Generic;
using Extensions;
using Extensions.Serialize;

namespace Modules.Localize
{
    public sealed class LocalizeSpriteAsset : ScriptableObject
    {
        //----- params -----

		// key : フォルダのguid,
		// value : フォルダパス.
		[Serializable]
		public sealed class FolderDictionary : SerializableDictionary<string, string> { }

		// key : spriteのguid
		// value : 所属するフォルダのguid.
		[Serializable]
		public sealed class SpriteDictionary : SerializableDictionary<string, string> { }

		[Serializable]
		public sealed class FolderInfo
		{
			public string guid = null;
			public string description = null;
		}

        //----- field -----

		[SerializeField]
		private FolderInfo[] infos = null;
		[SerializeField]
		private FolderDictionary folderDictionary = null;
		[SerializeField]
		private SpriteDictionary spriteDictionary = null;

		private Dictionary<string, string> folderPathCache = null;

		private AesCryptoKey cryptoKey = null;

		//----- property -----

		public IReadOnlyList<FolderInfo> Infos
		{
			get { return infos ?? (infos = new FolderInfo[0]); }
		}

        //----- method -----

		public void SetCryptoKey(AesCryptoKey cryptoKey)
		{
			this.cryptoKey = cryptoKey;

			folderPathCache = new Dictionary<string, string>();
		}

		public string GetFolderPath(string folderGuid)
		{
			if (string.IsNullOrEmpty(folderGuid)){ return string.Empty; }

			var folderPath = folderPathCache.GetValueOrDefault(folderGuid);
			
			if (string.IsNullOrEmpty(folderPath))
			{
				var encrypted = folderDictionary.GetValueOrDefault(folderGuid);
			
				folderPath = encrypted.Decrypt(cryptoKey);

				folderPath = folderPath.TrimEnd(PathUtility.PathSeparator);

				folderPathCache.Add(folderGuid, folderPath);
			}

			return folderPath;
		}

		public string GetAtlasFolderPath(string spriteGuid)
		{
			if (string.IsNullOrEmpty(spriteGuid)){ return string.Empty; }

			var folderGuid = spriteDictionary.GetValueOrDefault(spriteGuid);
			
			return GetFolderPath(folderGuid);
		}
	}
}