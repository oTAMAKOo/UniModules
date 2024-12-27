
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Extensions;
using MessagePack;
using Modules.LocalData;

namespace Modules.FileCache
{
    [Serializable]
    [MessagePackObject(true)]
    public sealed partial class CacheFileData
    {
        public string Source { get; private set; }

        public ulong UpdateAt { get; private set; }

        public ulong ExpireAt { get; private set; }

        [SerializationConstructor]
        public CacheFileData(string source, ulong updateAt, ulong expireAt)
        {
            this.Source = source;
            this.UpdateAt = updateAt;
            this.ExpireAt = expireAt;
        }

        public bool Alive()
        {
            var now = DateTime.Now.ToUnixTime();

            return now < ExpireAt;
        }
    }

    [FileName("__FileCache_")]
    [MessagePackObject(true)]
    public sealed class CacheData : ILocalData
    {
        public CacheFileData[] files = new CacheFileData[0];

        public CacheData() {}

        [SerializationConstructor]
        public CacheData(CacheFileData[] files)
        {
            this.files = files;
        }
    }

    public abstract class FileCacheManagerBase<TInstance> : Singleton<TInstance> where TInstance : FileCacheManagerBase<TInstance>
    {
        //----- params -----

        //----- field -----

        private AesCryptoKey cryptoKey = null;

        //----- property -----

        public string CacheDirectory { get; private set; }

        //----- method -----

        public void SetCryptoKey(AesCryptoKey cryptoKey)
        {
            this.cryptoKey = cryptoKey;
        }

        public void SetBaseDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory)) { return; }

            var className = typeof(TInstance).FullName;

            var hash = className.GetHash();

            CacheDirectory = PathUtility.Combine(directory, hash) + PathUtility.PathSeparator;

            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }
        }

        public bool HasCache(string source, ulong updateAt)
        {
            if (string.IsNullOrEmpty(source)){ return false; }

            var cacheData = LocalDataManager.Get<CacheData>();

            var data = cacheData.files.FirstOrDefault(x => x.Source == source);

            // キャッシュに存在しない.
            if (data == null){ return false; }

            // 有効期限切れ.
            if (!data.Alive()){ return false; }

            // キャッシュと更新日時が異なる.
            if (data.UpdateAt != updateAt){ return false; }

            var fileName = GetFileName(source);

            var filePath = PathUtility.Combine(CacheDirectory, fileName);

            // ファイルが存在しない.
            if (!File.Exists(filePath)){ return false; }

            return true;
        }

        public void CleanExpiredFiles()
        {
            if (string.IsNullOrEmpty(CacheDirectory)){ return; }

            if (!Directory.Exists(CacheDirectory)){ return; }

            var cacheData = LocalDataManager.Get<CacheData>();
            
            foreach (var fileData in cacheData.files)
            {
                var fileName = GetFileName(fileData.Source);

                var filePath = PathUtility.Combine(CacheDirectory, fileName);

                if (!File.Exists(filePath)){ continue; }

                if (fileData.Alive()){ continue; }

                File.Delete(filePath);
            }
        }

        protected void CreateCache(byte[] bytes, string source, ulong updateAt, ulong expireAt)
        {
            if (bytes.IsEmpty()){ return; }

            if (string.IsNullOrEmpty(CacheDirectory)){ return; }

            if (!Directory.Exists(CacheDirectory))
            {
                Directory.CreateDirectory(CacheDirectory);
            }

            var fileName = GetFileName(source);

            var filePath = PathUtility.Combine(CacheDirectory, fileName);

            bytes = bytes.Encrypt(cryptoKey);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fileStream.Write(bytes, 0, bytes.Length);
            }

            // キャッシュ情報更新.

            var cacheData = LocalDataManager.Get<CacheData>();

            if (cacheData != null)
            {
                var directory = cacheData.files.ToDictionary(x => x.Source);

                directory[source] = new CacheFileData(source, updateAt, expireAt);

                cacheData.files = directory.Values.ToArray();

                cacheData.Save();
            }
        }

        protected byte[] LoadCache(string source)
        {
            if (string.IsNullOrEmpty(CacheDirectory)){ return null; }

            var fileName = GetFileName(source);

            var filePath = PathUtility.Combine(CacheDirectory, fileName);

            if (!File.Exists(filePath)){ return null; }
            
            var bytes = new byte[0];

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                bytes = new byte[fileStream.Length];

                fileStream.Read(bytes, 0, bytes.Length);
            }

            bytes = bytes.Decrypt(cryptoKey);

            return bytes;
        }

        public string GetFileName(string source)
        {
            return string.IsNullOrEmpty(source) ? null : source.GetHash();
        }
    }
}