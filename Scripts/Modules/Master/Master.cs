
using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using UniRx;
using Extensions;
using MessagePack;
using Modules.MessagePack;

namespace Modules.Master
{
    public interface IMaster
    {
        string Version { get; }

        bool CheckVersion(string masterVersion);
        void ClearVersion();

        IObservable<bool> Update(string masterVersion, CancellationToken cancelToken);
        IObservable<bool> Load(AesManaged aesManaged);
    }

    public abstract class Master<TMaster, TMasterData> : IMaster where TMaster : Master<TMaster, TMasterData>, new()
    {
        //----- params -----

        private class Prefs
        {
            public string version
            {
                get { return PlayerPrefs.GetString(string.Format("{0}_VERSION", typeof(TMaster).Name.ToUpper())); }
                set { PlayerPrefs.SetString(string.Format("{0}_VERSION", typeof(TMaster).Name.ToUpper()), value); }
            }
        }

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
                    MasterManager.Instance.All.Add(instance);
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
                var message = "Master register error!\nRegistered keys can not be registered.";
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
            #if UNITY_EDITOR

            if (!MasterManager.Prefs.checkVersion) { return true; }

            #endif

            var result = true;

            var installPath = MasterManager.Instance.GetInstallPath<TMaster>();

            // ファイルがなかったらバージョン不一致.
            result &= File.Exists(installPath);

            // ローカル保存されているバージョンと一致するか.
            result &= versionPrefs.version == masterVersion;

            return result;
        }

        public void ClearVersion()
        {
            var installPath = MasterManager.Instance.GetInstallPath<TMaster>();

            if (File.Exists(installPath))
            {
                File.Delete(installPath);
            }

            versionPrefs.version = string.Empty;
            masters.Clear();
        }

        public IObservable<bool> Load(AesManaged aesManaged)
        {
            return Observable.FromMicroCoroutine<bool>(observer => LoadInternal(observer, aesManaged));
        }

        private IEnumerator LoadInternal(IObserver<bool> observer, AesManaged aesManaged)
        {
            var result = false;

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

            var installPath = MasterManager.Instance.GetInstallPath<TMaster>();

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

            if (!loadYield.HasError)
            {
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
            }
            else
            {
                Debug.LogException(loadYield.Error);
            }

            if (!result)
            {
                File.Delete(installPath);
                OnError();
            }

            observer.OnNext(result);
            observer.OnCompleted();
        }

        public IObservable<bool> Update(string masterVersion, CancellationToken cancelToken)
        {
            return Observable.FromMicroCoroutine<bool>(observer => UpdateInternal(observer, masterVersion, cancelToken));
        }

        private IEnumerator UpdateInternal(IObserver<bool> observer, string masterVersion, CancellationToken cancelToken)
        {
            var result = true;

            var installPath = MasterManager.Instance.GetInstallPath<TMaster>();

            // 既存のファイル削除.
            if (File.Exists(installPath))
            {
                File.Delete(installPath);
            }

            // ダウンロード.
            var downloadYield = DownloadMaster().ToYieldInstruction(false, cancelToken);

            while (!downloadYield.IsDone)
            {
                yield return null;
            }

            if (!downloadYield.HasResult || downloadYield.HasError || !File.Exists(installPath))
            {
                result = false;
            }

            // バージョン情報を更新.
            versionPrefs.version = result ? masterVersion : string.Empty;

            observer.OnNext(result);
            observer.OnCompleted();
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

        protected abstract IObservable<Unit> DownloadMaster();
    }
}
