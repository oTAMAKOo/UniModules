
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Console;

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

        public static readonly string ConsoleEventName = "LocalData";
        public static readonly Color ConsoleEventColor = new Color(0.85f, 0.55f, 0.1f);

        //----- field -----

        private AesCryptoKey cryptoKey = null;

        private HashSet<ILocalData> saveRequests = null;

        private Dictionary<Type, string> filePathCache = null;

        private Dictionary<Type, ILocalData> dataCache = null;

        private Subject<ILocalData> onLoad = null;

        private Subject<ILocalData> onSave = null;

        //----- property -----

        /// <summary> デフォルトのファイルディレクトリ </summary>
        public string DefaultFileDirectory { get; private set; }

        /// <summary> ファイルディレクトリ </summary>
        public string FileDirectory { get; private set; }

        //----- method -----

        protected override void OnCreate()
        {
            filePathCache = new Dictionary<Type, string>();
            dataCache = new Dictionary<Type, ILocalData>();
            saveRequests = new HashSet<ILocalData>();

            var fileDir = UnityPathUtility.GetPrivateDataPath();

            DefaultFileDirectory = fileDir + "/LocalData/";

            SetFileDirectory(DefaultFileDirectory);
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

        public static void Load<T>() where T : class, ILocalData, new()
        {
            var type = typeof(T);

            var className = typeof(T).FullName;
            var filePath = Instance.GetFilePath<T>();

            T data = null;

            try
            {
                data = MessagePackFileUtility.Read<T>(filePath, Instance.cryptoKey);

                if (data == null)
                {
                    data = new T();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"LocalData load failed.\nClass:{className}\nFilePath:{filePath}\n\n{ex.Message}", ex);
            }

            if (Instance.onLoad != null)
            {
                Instance.onLoad.OnNext(data);
            }

            Instance.dataCache[type] = data;

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, $"Load : {className}\nFilePath:{filePath}");
        }

        public static void Save<T>(T data) where T : class, ILocalData, new()
        {
            if (data == null) { return; }

            Instance.SaveOnEndOfFrame(data).Forget();
        }

        private async UniTask SaveOnEndOfFrame<T>(T data) where T : class, ILocalData, new()
        {
            if (saveRequests.Contains(data)){ return; }

            saveRequests.Add(data);

            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            WriteToFile(data);

            saveRequests.Remove(data);
        }

        private void WriteToFile<T>(T data) where T : class, ILocalData, new()
        {
            var type = data.GetType();
            var className = type.FullName;
            var filePath = GetFilePath<T>();

            try
            {
                MessagePackFileUtility.Write(filePath, data, Instance.cryptoKey);
            }
            catch (Exception ex)
            {
                throw new Exception($"LocalData save failed.\nClass:{className}\nFilePath:{filePath}\n\n{ex.Message}", ex);
            }

            if (onSave != null)
            {
                onSave.OnNext(data);
            }

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, $"Save : {className}\nFilePath:{filePath}");
        }

        public string GetFilePath<T>() where T : class, ILocalData, new()
        {
            var type = typeof(T);

            var isLocalData = type.GetInterfaces().Contains(typeof(ILocalData));

            if (!isLocalData)
            {
                throw new ArgumentException($"{type.FullName} has not ILocalData interface.");
            }

            var filePath = filePathCache.GetValueOrDefault(type);

            if (!string.IsNullOrEmpty(filePath)){ return filePath; }
            
            if (!Directory.Exists(FileDirectory))
            {
                Directory.CreateDirectory(FileDirectory);
            }

            var fileName = string.Empty;

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

            filePathCache.Add(type, filePath);

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
