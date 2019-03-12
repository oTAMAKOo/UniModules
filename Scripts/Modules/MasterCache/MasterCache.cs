
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
    public interface IMasterCache
    {
        string Version { get; }

        bool CheckVersion(string masterVersion);
        void ClearVersion();
        IObservable<bool> LoadCache(AesManaged aesManaged);
        IObservable<bool> UpdateCache(string masterVersion, AesManaged aesManaged, CancellationToken cancelToken);
    }

    public static class MasterCaches
    {
        private static List<IMasterCache> allMasterCaches;

        public static List<IMasterCache> All
        {
            get { return allMasterCaches ?? (allMasterCaches = new List<IMasterCache>()); }
        }
    }

    public abstract class MasterCache<TMaster, TMasterData> : IMasterCache where TMaster : MasterCache<TMaster, TMasterData>, new()
    {
        //----- params -----

        private const string CacheFileExtension = ".cache";

        private class Prefs
        {
            public string version
            {
                get { return PlayerPrefs.GetString(string.Format("{0}.Version", typeof(TMaster).Name.ToUpper())); }
                set { PlayerPrefs.SetString(string.Format("{0}.Version", typeof(TMaster).Name.ToUpper()), value); }
            }
        }

        private static readonly string ConsoleEventName = "MasterCache";
        private static readonly Color ConsoleEventColor = new Color(0.46f, 0.46f, 0.83f);

        //----- field -----

        private Dictionary<string, TMasterData> masters = new Dictionary<string, TMasterData>();
        private Prefs versionPrefs = new Prefs();

        private static TMaster instance = null;

        //----- property -----

        public static TMaster Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new TMaster();
                    MasterCaches.All.Add(instance);
                }

                return instance;
            }
        }

        /// <summary> バージョン. </summary>
        public string Version { get { return versionPrefs.version; } }

        //----- method -----

        public void SetMaster(TMasterData[] master)
        {
            masters.Clear();

            if (master == null) { return; }

            foreach (var item in master)
            {
                SetMaster(item);
            }
        }

        private void SetMaster(TMasterData master)
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

        public bool CheckVersion(string masterVersion)
        {
            var result = true;

            var installPath = PathUtility.Combine(GetInstallDirectory(), GetCacheFileName());
            
            // ファイルがなかったらバージョン不一致.
            result &= File.Exists(installPath);

            // ローカル保存されているバージョンと一致するか.
            result &= versionPrefs.version == masterVersion;

            return result;
        }

        public void ClearVersion()
        {
            var installPath = PathUtility.Combine(GetInstallDirectory(), GetCacheFileName());

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
                MessagePackValidater.ValidateAttribute(typeof(TMasterData));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            #endif

            var installPath = PathUtility.Combine(GetInstallDirectory(), GetCacheFileName());

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
                var bytes = loadYield.Result;

                try
                {
                    var data = LZ4MessagePackSerializer.Deserialize<TMasterData[]>(bytes, UnityContractResolver.Instance);

                    SetMaster(data);

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
                MasterCacheDiagnostic.Instance.Register<TMaster>(sw.Elapsed.TotalMilliseconds);
            }
            else
            {
                File.Delete(installPath);
                OnError();
            }

            observer.OnNext(result);
            observer.OnCompleted();
        }

        public IObservable<bool> UpdateCache(string masterVersion, AesManaged aesManaged, CancellationToken cancelToken)
        {
            return Observable.FromMicroCoroutine<bool>(observer => UpdateCacheInternal(observer, masterVersion, aesManaged, cancelToken));
        }

        private IEnumerator UpdateCacheInternal(IObserver<bool> observer, string masterVersion, AesManaged aesManaged, CancellationToken cancelToken)
        {
            var result = true;

            var localVersion = versionPrefs.version;

            var cachefilePath = PathUtility.Combine(GetInstallDirectory(), GetCacheFileName());

            // 既存のファイル削除.
            if (File.Exists(cachefilePath))
            {
                File.Delete(cachefilePath);
            }

            // ダウンロード.
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var downloadYield = UpdateMaster().ToYieldInstruction(false, cancelToken);

            while (!downloadYield.IsDone)
            {
                yield return null;
            }

            var downloadfilePath = PathUtility.Combine(GetInstallDirectory(), GetDownloadFileName());

            if (downloadYield.HasResult && !downloadYield.HasError && File.Exists(downloadfilePath))
            {
                // 拡張子を変更.
                File.Move(downloadfilePath, cachefilePath);

                sw.Stop();

                var message = string.Format("[{0}] Version : {1} >> {2}", typeof(TMaster).Name, string.IsNullOrEmpty(localVersion) ? "---" : localVersion, masterVersion);

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
            }
            else
            {
                result = false;
            }

            // 読み込み.
            if (result)
            {
                var loadYield = LoadCache(aesManaged).ToYieldInstruction(false, cancelToken);

                while (!loadYield.IsDone)
                {
                    yield return null;
                }

                if (loadYield.HasResult && !loadYield.HasError)
                {
                    result = loadYield.Result;
                }
                else
                {
                    result = false;
                }                
            }

            // バージョン情報を更新.
            versionPrefs.version = result ? masterVersion : string.Empty;

            observer.OnNext(result);
            observer.OnCompleted();
        }

        protected virtual string GetInstallDirectory()
        {
            return Application.temporaryCachePath;
        }

        private string GetCacheFileName()
        {
            return typeof(TMaster).Name + CacheFileExtension;
        }

        public IEnumerable<TMasterData> GetAllMasters()
        {
            return masters.Values;
        }

        public TMasterData GetMaster(string key)
        {
            return masters.GetValueOrDefault(key);
        }

        protected virtual void OnError()
        {
            // 強制更新させる為バージョン情報を削除.
            ClearVersion();
        }

        protected abstract string GetMasterKey(TMasterData master);

        protected abstract string GetDownloadFileName();

        protected abstract IObservable<Unit> UpdateMaster();
    }
}
