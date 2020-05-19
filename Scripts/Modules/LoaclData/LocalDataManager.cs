
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

        private const string AESKey = "5k7DpsG19A91R7Lkv261AMxCmjHFFFxX";
        private const string AESIv = "YiEs3x1as8JhK9qp";

        //----- field -----

        private AesManaged aesManaged = null;

        private Dictionary<Type, string> filePathDictionary = null;

        //----- property -----

        //----- method -----

        protected override void OnCreate()
        {
            aesManaged = AESExtension.CreateAesManaged(AESKey, AESIv);

            filePathDictionary = new Dictionary<Type, string>();
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

                    bytes = bytes.Decrypt(Instance.aesManaged);

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

                bytes = bytes.Encrypt(Instance.aesManaged);

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

                var fileName = tempInstance.FileName.Encrypt(aesManaged).GetHash();

                filePath = directory + fileName;

                filePathDictionary.Add(typeof(T), filePath);
            }

            return filePath;
        }

        public static void DeleteAll()
        {
            var directory = GetFileDirectory();

            DirectoryUtility.Clean(directory);
        }
    }
}
