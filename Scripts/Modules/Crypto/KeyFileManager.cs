
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Extensions;

namespace Modules.Crypto
{
    public sealed class KeyData
    {
        public string Key { get; private set; }

        public string Iv { get; private set; }

        public KeyData(string key, string iv)
        {
            Key = key;
            Iv = iv;
        }
    }

    public interface IKeyFileManager<TKeyType> where TKeyType : Enum
    {
		string ResourcesDirectory { get; }

		void Create(string filePath, string key, string iv);

		void Load();

		void ClearCache();

		KeyData Get(TKeyType keyType);

		string GetResourcesPath(TKeyType keyType);
	}

    public abstract class KeyFileManager<TInstance, TKeyType> : Singleton<TInstance>, IKeyFileManager<TKeyType>
		where TInstance : KeyFileManager<TInstance, TKeyType>
		where TKeyType : Enum
    {
        //----- params -----

        //----- field -----

		private Dictionary<TKeyType, KeyData> keyCache = null;

        //----- property -----

		public abstract string ResourcesDirectory { get; }

		protected abstract string Separator { get; }

        //----- method -----

		protected override void OnCreate()
		{
			keyCache = new Dictionary<TKeyType, KeyData>();
		}

        public void Create(string filePath, string key, string iv)
        {
            var directory = Path.GetDirectoryName(filePath);

            if (Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var guid = Guid.NewGuid().ToString();

            var aesKey = new AesCryptoKey(guid);

            var encryptKey = key.Encrypt(aesKey);

            var encryptIv = iv.Encrypt(aesKey);

            var str = encryptKey + Separator + encryptIv + Separator + guid;

            var bytes = Encoding.UTF8.GetBytes(str);

            bytes = CustomEncode(bytes);

            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                file.Write(bytes, 0, bytes.Length);
            }
        }

		public void Load()
		{
			keyCache = new Dictionary<TKeyType, KeyData>();

			var keyTypes = Enum.GetValues(typeof(TKeyType)).Cast<TKeyType>();
			
			foreach (var keyType in keyTypes)
			{
				var resourcesPath = GetResourcesPath(keyType);

				var loadPath = PathUtility.GetPathWithoutExtension(resourcesPath);

				var asset = Resources.Load(loadPath) as TextAsset;

				if (asset != null)
				{
					var bytes = asset.bytes;

					if(bytes != null && bytes.Any())
					{
						bytes = CustomDecode(bytes);

						var str = Encoding.UTF8.GetString(bytes);

						var parts = str.Split(new string[]{ Separator }, StringSplitOptions.None);

						var encryptKey = parts.ElementAtOrDefault(0, string.Empty);
						var encryptIv = parts.ElementAtOrDefault(1, string.Empty);
						var fileKey = parts.ElementAtOrDefault(2, string.Empty);

						if (string.IsNullOrEmpty(encryptKey) || string.IsNullOrEmpty(encryptIv) || string.IsNullOrEmpty(fileKey))
						{
							throw new InvalidDataException(resourcesPath);
						}

						var aesKey = new AesCryptoKey(fileKey);

						var key = encryptKey.Decrypt(aesKey);
						var iv = encryptIv.Decrypt(aesKey);
			
						keyCache.Add(keyType, new KeyData(key, iv));
					}
				}
			}
		}

		public KeyData Get(TKeyType keyType)
		{
			return keyCache.GetValueOrDefault(keyType);
		}

		public string GetResourcesPath(TKeyType keyType)
		{
			var keyName = Enum.GetName(typeof(TKeyType), keyType);

			var fileName = keyName.GetHash() + ".bytes";

			return PathUtility.Combine(ResourcesDirectory, fileName);
		}

		public void ClearCache()
		{
			if (keyCache != null)
			{
				keyCache.Clear();
			}
		}

		protected virtual byte[] CustomEncode(byte[] bytes) { return bytes; }

        protected virtual byte[] CustomDecode(byte[] bytes) { return bytes; }
    }
}
