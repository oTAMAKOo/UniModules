
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

		UniTask Load();

		void ClearCache();

		KeyData Get(TKeyType keyType);

		string GetLoadPath(TKeyType keyType);
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

		public async UniTask Load()
		{
			keyCache = new Dictionary<TKeyType, KeyData>();

			var keyTypes = Enum.GetValues(typeof(TKeyType)).Cast<TKeyType>();
			
			var tasks = new List<UniTask>();

			foreach (var keyType in keyTypes)
			{
				var task = UniTask.Defer(() => LoadKeyFile(keyType));

				tasks.Add(task);
			}

			await UniTask.WhenAll(tasks);
		}
		
		public async UniTask LoadKeyFile(TKeyType keyType)
		{
			byte[] bytes = null;

			var loadPath = GetLoadPath(keyType);

            var streamingAssetPath = UnityPathUtility.StreamingAssetsPath;

			var filePath = PathUtility.Combine(streamingAssetPath, loadPath);

			// ※ AndroidではstreamingAssetsPathがWebRequestからしかアクセスできないのでtemporaryCachePathにファイルを複製する.

			#if UNITY_ANDROID && !UNITY_EDITOR

			await AndroidUtility.CopyStreamingToTemporary(filePath);

			filePath = AndroidUtility.ConvertStreamingAssetsLoadPath(filePath);

			#endif

			if (!File.Exists(filePath)){ return; }

			using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				using (var memoryStream = new MemoryStream())
				{
					await fileStream.CopyToAsync(memoryStream);

					bytes = memoryStream.ToArray();
				}
			}
			
			if (bytes.Any())
			{
				bytes = CustomDecode(bytes);

				var str = Encoding.UTF8.GetString(bytes);

				var parts = str.Split(new string[]{ Separator }, StringSplitOptions.None);

				var encryptKey = parts.ElementAtOrDefault(0, string.Empty);
				var encryptIv = parts.ElementAtOrDefault(1, string.Empty);
				var fileKey = parts.ElementAtOrDefault(2, string.Empty);

				if (string.IsNullOrEmpty(encryptKey) || string.IsNullOrEmpty(encryptIv) || string.IsNullOrEmpty(fileKey))
				{
					throw new InvalidDataException(filePath);
				}

				var aesKey = new AesCryptoKey(fileKey);

				var key = encryptKey.Decrypt(aesKey);
				var iv = encryptIv.Decrypt(aesKey);
			
				keyCache[keyType] = new KeyData(key, iv);
			}
		}

		public KeyData Get(TKeyType keyType)
		{
			return keyCache.GetValueOrDefault(keyType);
		}

		public string GetLoadPath(TKeyType keyType)
		{
			var keyName = Enum.GetName(typeof(TKeyType), keyType);

			var fileName = keyName.GetHash();

			return PathUtility.Combine(FileDirectory, fileName);
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
