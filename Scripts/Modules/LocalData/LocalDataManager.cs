
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using MessagePack;
using MessagePack.Resolvers;
using Modules.MessagePack;

namespace Modules.LocalData
{
    public interface ILocalData { }

    public static class LocalDataExtension
    {
        public static void Save<T>(this T data) where T : class, ILocalData, new()
        {
            LocalDataManager.Save(data);
        }
    }

    public sealed class LocalDataManager : Singleton<LocalDataManager>
    {
        //----- params -----

        //----- field -----

        private AesCryptoKey cryptoKey = null;

        private Dictionary<Type, string> filePathCache = null;

        private Dictionary<Type, ILocalData> dataCache = null;

        private Subject<ILocalData> onLoad = null;

        private Subject<ILocalData> onSave = null;

        //----- property -----

        public string BaseDirectory { get; private set; }

        public string FileDirectory { get; private set; }

        //----- method -----

        protected override void OnCreate()
        {
            filePathCache = new Dictionary<Type, string>();
            dataCache = new Dictionary<Type, ILocalData>();

            var fileDir = UnityPathUtility.GetPrivateDataPath();

            BaseDirectory = fileDir + "/LocalData/";

            SetFileDirectory(BaseDirectory);
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

        public void CacheClear()
        {
            if (filePathCache != null)
            {
                filePathCache.Clear();
            }

            if (dataCache != null)
            {
                dataCache.Clear();
            }
        }

        public static void Load<T>() where T : class, ILocalData, new()
        {
            var type = typeof(T);

            var filePath = Instance.GetFilePath<T>();

            var data = new T();

            if (File.Exists(filePath))
            {
                byte[] bytes = null;

                try
                {
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        bytes = new byte[fileStream.Length];

                        fileStream.Read(bytes, 0, bytes.Length);
                    }

                    if (!bytes.IsEmpty())
                    {
                        bytes = bytes.Decrypt(Instance.cryptoKey);

                        var options = StandardResolverAllowPrivate.Options
                            .WithCompression(MessagePackCompression.Lz4BlockArray)
                            .WithResolver(UnityCustomResolver.Instance);

                        data = MessagePackSerializer.Deserialize<T>(bytes, options);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"LocalData load failed.\nClass:{typeof(T).FullName}\nFilePath:{filePath}", ex);
                }

                if (Instance.onLoad != null)
                {
                    Instance.onLoad.OnNext(data);
                }
            }

            Instance.dataCache[type] = data;
        }

        public static T Get<T>() where T : class, ILocalData, new()
        {
            var type = typeof(T);

            var dataCache = Instance.dataCache;

            if (!dataCache.ContainsKey(type))
            {
                Load<T>();
            }

            return dataCache.GetValueOrDefault(typeof(T)) as T;
        }

        public static void Save<T>(T data) where T : class, ILocalData, new()
        {
            var filePath = Instance.GetFilePath<T>();

            try
            {
                var options = StandardResolverAllowPrivate.Options
                    .WithCompression(MessagePackCompression.Lz4BlockArray)
                    .WithResolver(UnityCustomResolver.Instance);

                var bytes = MessagePackSerializer.Serialize(data, options);

                bytes = bytes.Encrypt(Instance.cryptoKey);

                using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    fileStream.Write(bytes, 0, bytes.Length);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"LocalData save failed.\nClass:{typeof(T).FullName}\nFilePath:{filePath}", ex);
            }

            if (Instance.onSave != null)
            {
                Instance.onSave.OnNext(data);
            }
        }

        public string GetFilePath<T>() where T : class, ILocalData, new()
        {
            var filePath = filePathCache.GetValueOrDefault(typeof(T));

            if (!string.IsNullOrEmpty(filePath)){ return filePath; }
            
            if (!Directory.Exists(FileDirectory))
            {
                Directory.CreateDirectory(FileDirectory);
            }

            var fileName = string.Empty;

            var type = typeof(T);

            var fileNameAttribute = type.GetCustomAttributes(typeof(FileNameAttribute), false)
                .Cast<FileNameAttribute>()
                .FirstOrDefault();

            if (fileNameAttribute != null)
            {
                fileName = fileNameAttribute.Encrypt ? 
                           fileNameAttribute.FileName.Encrypt(cryptoKey).GetHash() : 
                           fileNameAttribute.FileName;
            }
            else
            {
                throw new Exception($"FileNameAttribute is not set for this class.\n{type.FullName}");
            }

            filePath = PathUtility.Combine(FileDirectory, fileName);

            filePathCache.Add(typeof(T), filePath);

            return filePath;
        }

        public void SetCryptoKey(AesCryptoKey cryptoKey)
        {
            this.cryptoKey = cryptoKey;
        }

        public static void Delete<T>() where T : class, ILocalData, new()
        {
            var type = typeof(T);

            var filePath = Instance.GetFilePath<T>();
            var dataCache = Instance.dataCache;

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            if (dataCache != null && dataCache.ContainsKey(type))
            {
                dataCache.Remove(type);
            }
        }

        public static void DeleteAll()
        {
            var directory = Instance.FileDirectory;
            var dataCache = Instance.dataCache;

            DirectoryUtility.Clean(directory);

            if (dataCache != null)
            {
                dataCache.Clear();
            }
        }

        public IObservable<ILocalData> OnLoadAsObservable()
        {
            return onLoad ?? (onLoad = new Subject<ILocalData>());
        }

        public IObservable<ILocalData> OnSaveAsObservable()
        {
            return onSave ?? (onSave = new Subject<ILocalData>());
        }
    }
}
