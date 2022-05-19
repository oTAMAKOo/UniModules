
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
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
		string FileDirectory { get; }

		void Create(string filePath, string key, string iv);

		KeyData Get(TKeyType keyType);

		string GetFileName(TKeyType keyType);

		void ClearCache();
	}

    public abstract class KeyFileManager<TInstance, TKeyType> : Singleton<TInstance>, IKeyFileManager<TKeyType>
		where TInstance : KeyFileManager<TInstance, TKeyType>
		where TKeyType : Enum
    {
        //----- params -----

        //----- field -----

		private Dictionary<TKeyType, KeyData> keyCache = null;

        //----- property -----

		public abstract string FileDirectory { get; }

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

        private KeyData Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return new KeyData(string.Empty, string.Empty);
            }

            var bytes = new byte[0];

            using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var memoryStream = new MemoryStream())
                {
                    fileStream.CopyTo(memoryStream);

                    bytes = memoryStream.ToArray();
                }
            }

            if (bytes == null || bytes.IsEmpty()){ return null; }

            bytes = CustomDecode(bytes);

            var str = Encoding.UTF8.GetString(bytes);

            var parts = str.Split(new string[]{ Separator }, StringSplitOptions.None);

            var encryptKey = parts.ElementAtOrDefault(0, string.Empty);
            var encryptIv = parts.ElementAtOrDefault(1, string.Empty);
            var fileKey = parts.ElementAtOrDefault(2, string.Empty);

            if (string.IsNullOrEmpty(encryptKey) || string.IsNullOrEmpty(encryptIv) || string.IsNullOrEmpty(fileKey))
            {
                return null;
            }

            var aesKey = new AesCryptoKey(fileKey);

            var key = encryptKey.Decrypt(aesKey);
            var iv = encryptIv.Decrypt(aesKey);

            return new KeyData(key, iv);
        }

		#pragma warning disable CS4014

		public KeyData Get(TKeyType keyType)
		{
			var keyData = keyCache.GetValueOrDefault(keyType);

			if (keyData != null){ return keyData; }

			var fileName = GetFileName(keyType);

			var filePath = PathUtility.Combine(FileDirectory, fileName);

			// ※ AndroidではstreamingAssetsPathがWebRequestからしかアクセスできないのでtemporaryCachePathにファイルを複製する.

			#if UNITY_ANDROID && !UNITY_EDITOR

			if (filePath.StartsWith(UnityPathUtility.StreamingAssetsPath))
			{
				AndroidUtility.CopyStreamingToTemporary(filePath);
			}
			
			#endif

			keyData = Load(filePath);
			
			keyCache.Add(keyType, keyData);

			return keyData;
		}

		#pragma warning restore CS4014
		
		public string GetFileName(TKeyType keyType)
		{
			var keyName = Enum.GetName(typeof(TKeyType), keyType);

			return keyName.GetHash();
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
