
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using Extensions;
using MessagePack;
using MessagePack.Resolvers;
using Modules.MessagePack;

namespace Modules.LocalData
{
    [MessagePackObject(true)]
    public abstract class LocalData
    {
        public abstract string FileName { get; }        
    }

    public static class LocalDataExtension
    {
        public static void Save<T>(this T data) where T : LocalData, new()
        {
            LocalDataManager.Save(data);
        }
    }

    public sealed class LocalDataManager : Singleton<LocalDataManager>
    {
        //----- params -----

        private const string DefaultKey = "5k7DpsG19A91R7Lkv261AMxCmjHFFFxX";
        private const string DefaultIv = "YiEs3x1as8JhK9qp";

        //----- field -----

        private AesManaged aesManaged = null;

        private Dictionary<Type, string> filePathDictionary = null;

        //----- property -----

        //----- method -----

        protected override void OnCreate()
        {
            filePathDictionary = new Dictionary<Type, string>();
        }

        public static void SetAesManaged(AesManaged aesManaged)
        {
            Instance.aesManaged = aesManaged;
        }

        public static T Get<T>() where T : LocalData, new()
        {
            T result = null;

            MessagePackValidater.ValidateAttribute(typeof(T));

            var filePath = Instance.GetLocalDataFilePath<T>();

            if (File.Exists(filePath))
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    var bytes = new byte[fileStream.Length];

                    fileStream.Read(bytes, 0, bytes.Length);

                    var aesManaged = Instance.GetAesManaged();

                    bytes = bytes.Decrypt(aesManaged);

                    var options = StandardResolverAllowPrivate.Options
                        .WithCompression(MessagePackCompression.Lz4BlockArray)
                        .WithResolver(UnityContractResolver.Instance);

                    result = MessagePackSerializer.Deserialize<T>(bytes, options);
                }
            }

            if (result == null)
            {
                result = new T();
            }

            return result;
        }

        public static void Save<T>(T data) where T : LocalData, new()
        {
            MessagePackValidater.ValidateAttribute(typeof(T));

            var filePath = Instance.GetLocalDataFilePath<T>();

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                var options = StandardResolverAllowPrivate.Options
                    .WithCompression(MessagePackCompression.Lz4BlockArray)
                    .WithResolver(UnityContractResolver.Instance);

                var bytes = MessagePackSerializer.Serialize(data, options);

                var aesManaged = Instance.GetAesManaged();

                bytes = bytes.Encrypt(aesManaged);

                fileStream.Write(bytes, 0, bytes.Length);
            }
        }

        private static string GetFileDirectory()
        {
            return Application.persistentDataPath + "/LocalData/";
        }

        private string GetLocalDataFilePath<T>() where T : LocalData, new()
        {
            var filePath = filePathDictionary.GetValueOrDefault(typeof(T));

            if (string.IsNullOrEmpty(filePath))
            {
                var tempInstance = new T();

                var directory = GetFileDirectory();

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var aesManaged = GetAesManaged();

                var fileName = tempInstance.FileName.Encrypt(aesManaged).GetHash();

                filePath = directory + fileName;

                filePathDictionary.Add(typeof(T), filePath);
            }

            return filePath;
        }

        private AesManaged GetAesManaged()
        {
            if (aesManaged == null)
            {
                aesManaged = AESExtension.CreateAesManaged(DefaultKey, DefaultIv);
            }

            return aesManaged;
        }

        public static void DeleteAll()
        {
            var directory = GetFileDirectory();

            DirectoryUtility.Clean(directory);
        }
    }
}
