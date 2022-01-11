
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Extensions;
using MessagePack;
using Modules.LocalData;

namespace Modules.FileCache
{
    [MessagePackObject(true)]
    public sealed class CacheFileData
    {
        public string Source { get; private set; }

        public long UpdateAt { get; private set; }

        public long ExpireAt { get; private set; }

        public CacheFileData(string source, long updateAt, long expireAt)
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
        public CacheFileData[] files = null;
    }

    public abstract class FileCacheManagerBase<TInstance> : Singleton<TInstance> where TInstance : FileCacheManagerBase<TInstance>
    {
        //----- params -----

        //----- field -----

        private AesCryptoKey cryptoKey = null;

        private Dictionary<string, CacheFileData> cacheContents = null;

        //----- property -----

        public string FileDirectory { get; private set; }

        //----- method -----

        protected override void OnCreate()
        {
            FileDirectory = PathUtility.Combine(Application.temporaryCachePath, "FileCache") + PathUtility.PathSeparator;
        }

        public void SetCryptoKey(AesCryptoKey cryptoKey)
        {
            this.cryptoKey = cryptoKey;
        }

        public void SetFileDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory)) { return; }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            FileDirectory = directory;
        }

        public bool HasCache(string source, long updateAt)
        {
            if (string.IsNullOrEmpty(source)){ return false; }

            LoadCacheContents();

            var data = cacheContents.GetValueOrDefault(source);

            // �L���b�V���ɑ��݂��Ȃ�.
            if (data == null){ return false; }

            // �L�������؂�.
            if (!data.Alive()){ return false; }

            // �L���b�V�����V����.
            if (data.UpdateAt < updateAt){ return false; }

            var fileName = GetFileName(source);

            var filePath = PathUtility.Combine(FileDirectory, fileName);

            // �t�@�C�������݂��Ȃ�.
            if (!File.Exists(filePath)){ return false; }

            return true;
        }

        public void CleanExpiredFiles()
        {
            LoadCacheContents();
            
            foreach (var fileData in cacheContents.Values)
            {
                var fileName = GetFileName(fileData.Source);

                var filePath = PathUtility.Combine(FileDirectory, fileName);

                if (!File.Exists(filePath)){ continue; }

                if (fileData.Alive()){ continue; }

                File.Delete(filePath);
            }
        }

        protected void CreateCache(byte[] bytes, string source, long updateAt, long expireAt)
        {
            // �t�@�C���o��.

            var fileName = GetFileName(source);

            var filePath = PathUtility.Combine(FileDirectory, fileName);

            bytes = bytes.Encrypt(cryptoKey);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fileStream.Write(bytes, 0, bytes.Length);
            }

            // �L���b�V�����X�V.

            LoadCacheContents();

            cacheContents[source] = new CacheFileData(source, updateAt, expireAt);

            var cacheData = new CacheData()
            {
                files = cacheContents.Values.ToArray(),
            };

            cacheData.Save();
        }

        protected byte[] LoadCache(string source)
        {
            var fileName = GetFileName(source);

            var filePath = PathUtility.Combine(FileDirectory, fileName);

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

        private void LoadCacheContents()
        {
            if (cacheContents != null){ return; }
            
            var cacheData = LocalDataManager.Get<CacheData>();

            if (cacheData.files == null)
            {
                cacheData.files = new CacheFileData[0];
            }

            cacheContents = cacheData.files.ToDictionary(x => x.Source);
        }

        private string GetFileName(string source)
        {
            return string.IsNullOrEmpty(source) ? null : source.GetHash();
        }
    }
}