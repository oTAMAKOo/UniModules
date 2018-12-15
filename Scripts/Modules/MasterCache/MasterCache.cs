
using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using UniRx;
using Extensions;
using MessagePack;
using Modules.Devkit;
using Modules.MessagePack;

namespace Modules.MasterCache
{
    [MessagePackObject(true)]
    public abstract class Cache<T>
    {
        public T[] values = new T[0];
    }

    public interface IMasterCache
    {
        string Version { get; }

        bool CheckVersion(string applicationVersion, string masterVersion);
        void ClearVersion();
        IObservable<bool> LoadCache(AesManaged aesManaged);
        IObservable<bool> UpdateCache(string applicationVersion, string masterVersion, AesManaged aesManaged, CancellationToken cancelToken);
    }

    public static class MasterCaches
    {
        private static List<IMasterCache> allMasterCaches;

        public static List<IMasterCache> All
        {
            get { return allMasterCaches ?? (allMasterCaches = new List<IMasterCache>()); }
        }
    }

    public abstract class MasterCache<TInstance, T, TCache> : IMasterCache
        where TInstance : MasterCache<TInstance, T, TCache>, new()
        where TCache : Cache<T>, new()
    {
        //----- params -----

        private const string MasterCacheExtension = ".cache";

        private class Prefs
        {
            public string version
            {
                get { return PlayerPrefs.GetString(string.Format("{0}.Version", typeof(TInstance).Name.ToUpper())); }
                set { PlayerPrefs.SetString(string.Format("{0}.Version", typeof(TInstance).Name.ToUpper()), value); }
            }
        }

        private static readonly string ConsoleEventName = "MasterCache";
        private static readonly Color ConsoleEventColor = new Color(0.46f, 0.46f, 0.83f);

        //----- field -----

        private Dictionary<string, T> masters = new Dictionary<string, T>();
        private Prefs versionPrefs = new Prefs();

        private static TInstance instance = null;

        //----- property -----

        public static TInstance Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TInstance();
                    MasterCaches.All.Add(instance);
                }

                return instance;
            }
        }

        public string Version
        {
            get { return versionPrefs.version; }
        }

        //----- method -----

        public void SetMaster(T[] master)
        {
            masters.Clear();

            if (master == null) { return; }

            foreach (var item in master)
            {
                SetMaster(item);
            }
        }

        private void SetMaster(T master)
        {
            if (master == null) { return; }

            var key = GetMasterKey(master);

            if (masters.ContainsKey(key))
            {
                var message = "MasterCache register error!\nRegistered keys can not be registered.";
                var typeName = master.GetType().FullName;

                Debug.LogErrorFormat("{0}\n\nMaster : {1}\nKey : {2}\n", message, typeName, key);
            }
            else
            {
                masters.Add(key, master);
            }
        }

        public bool CheckVersion(string applicationVersion, string masterVersion)
        {
            var result = true;

            var installPath = GetLocalCacheFilePath();

            // バージョン文字列.
            var version = ConvertVersionStr(applicationVersion, masterVersion);

            // ファイルがなかったらバージョン不一致.
            result &= File.Exists(installPath);

            // ローカル保存されているバージョンと一致するか.
            result &= versionPrefs.version == version;

            return result;
        }

        public void ClearVersion()
        {
            var installPath = GetLocalCacheFilePath();

            if (File.Exists(installPath))
            {
                File.Delete(installPath);
            }

            versionPrefs.version = string.Empty;
            masters.Clear();
        }

        public IObservable<bool> LoadCache(AesManaged aesManaged)
        {
            return Observable.FromMicroCoroutine<bool>(observer => LoadCacheInternal(observer, aesManaged));
        }

        private IEnumerator LoadCacheInternal(IObserver<bool> observer, AesManaged aesManaged)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            #if UNITY_EDITOR

            try
            {
                MessagePackValidater.ValidateAttribute(typeof(TCache));
                MessagePackValidater.ValidateAttribute(typeof(T));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            #endif

            var installPath = GetLocalCacheFilePath();

            Func<string, AesManaged, byte[]> loadCacheFile = (_installPath, _aesManaged) =>
            {
                // ファイル読み込み.
                var data = File.ReadAllBytes(_installPath);

                // 復号化.               
                return data.Decrypt(_aesManaged);
            };

            // ファイルの読み込みと復号化をスレッドプールで実行.
            var loadYield = Observable.Start(() => loadCacheFile(installPath, aesManaged)).ObserveOnMainThread().ToYieldInstruction();

            while (!loadYield.IsDone)
            {
                yield return null;
            }

            var result = false;

            if (loadYield.HasResult)
            {
                var data = loadYield.Result;

                try
                {
                    var cachedData = LZ4MessagePackSerializer.Deserialize<TCache>(data, UnityContractResolver.Instance);

                    SetMaster(cachedData.values);

                    result = true;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);      
                }
            }

            sw.Stop();
            
            if (result)
            {
                MasterCacheLoadDiagnostic.Instance.Register<TInstance>(sw.Elapsed.TotalMilliseconds);
            }
            else
            {
                File.Delete(installPath);
                OnError();
            }

            observer.OnNext(result);
            observer.OnCompleted();
        }

        public IObservable<bool> UpdateCache(string applicationVersion, string masterVersion, AesManaged aesManaged, CancellationToken cancelToken)
        {
            return Observable.FromMicroCoroutine<bool>(observer => UpdateCacheInternal(observer, applicationVersion, masterVersion, aesManaged, cancelToken));
        }

        private IEnumerator UpdateCacheInternal(IObserver<bool> observer, string applicationVersion, string masterVersion, AesManaged aesManaged, CancellationToken cancelToken)
        {
            var result = true;

            var localVersion = versionPrefs.version;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var updateYield = UpdateMaster().ToYieldInstruction(false, cancelToken);

            while (!updateYield.IsDone)
            {
                yield return null;
            }

            if (updateYield.HasResult && !updateYield.HasError)
            {
                result &= SaveCache(updateYield.Result, applicationVersion, masterVersion, aesManaged);

                sw.Stop();

                if (result)
                {
                    var version = ConvertVersionStr(applicationVersion, masterVersion);

                    var message = string.Format("[{0}] Version : {1} >>> {2}", typeof(TInstance).Name, string.IsNullOrEmpty(localVersion) ? "---" : localVersion, version);

                    UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);

                    MasterCacheUpdateDiagnostic.Instance.Register<TInstance>(sw.Elapsed.TotalMilliseconds);
                }
            }
            else
            {
                result = false;
            }

            observer.OnNext(result);
            observer.OnCompleted();
        }

        private bool SaveCache(object[] masterData, string applicationVersion, string masterVersion, AesManaged aesManaged)
        {
            try
            {
                var master = masterData.Select(x => (T)Activator.CreateInstance(typeof(T), x)).ToArray();

                SetMaster(master);

                var installPath = GetLocalCacheFilePath();

                var directory = Path.GetDirectoryName(installPath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                #if UNITY_EDITOR

                MessagePackValidater.ValidateAttribute(typeof(TCache));
                MessagePackValidater.ValidateAttribute(typeof(T));

                #endif

                var cacheData = new TCache() { values = masters.Values.ToArray() };

                var data = LZ4MessagePackSerializer.Serialize(cacheData, UnityContractResolver.Instance);
                var encrypt = data.Encrypt(aesManaged);

                File.WriteAllBytes(installPath, encrypt);

                versionPrefs.version = ConvertVersionStr(applicationVersion, masterVersion);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                OnError();

                return false;
            }

            return true;
        }

        protected static string GetLocalCacheFilePath()
        {
            var installDir = Application.temporaryCachePath;
            var fileName = typeof(TInstance).Name.ToLower() + MasterCacheExtension;

            return PathUtility.Combine(installDir, fileName);
        }

        private string ConvertVersionStr(string applicationVersion, string masterVersion)
        {
            return string.Format("{0}::{1}", applicationVersion, masterVersion);
        }

        public IEnumerable<T> GetAllMasters()
        {
            return masters.Values;
        }

        public T GetMaster(string key)
        {
            return masters.GetValueOrDefault(key);
        }

        protected virtual void OnError()
        {
            // 強制更新させる為バージョン情報を削除.
            ClearVersion();
        }

        protected abstract string GetMasterKey(T master);

        protected abstract IObservable<object[]> UpdateMaster();
    }
}
