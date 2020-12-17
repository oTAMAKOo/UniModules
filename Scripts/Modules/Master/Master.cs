
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
using MessagePack.Resolvers;
using Modules.MessagePack;

namespace Modules.Master
{
    public interface IMaster
    {
        string Version { get; }

        bool CheckVersion(string masterVersion);
        void ClearVersion();

        IObservable<bool> Update(string masterVersion, CancellationToken cancelToken);
        IObservable<bool> Load(AesManaged aesManaged, bool cleanOnError);
    }

    public abstract class MasterContainer<TMasterRecord>
    {
        public TMasterRecord[] records = null;
    }

    public abstract class Master<TKey, TMaster, TMasterContainer, TMasterRecord> : IMaster
        where TKey : IConvertible
        where TMaster : Master<TKey, TMaster, TMasterContainer, TMasterRecord>, new()
        where TMasterContainer : MasterContainer<TMasterRecord>
    {
        //----- params -----

        private sealed class Prefs
        {
            public string version
            {
                get { return PlayerPrefs.GetString(string.Format("{0}_VERSION", typeof(TMaster).Name.ToUpper())); }
                set { PlayerPrefs.SetString(string.Format("{0}_VERSION", typeof(TMaster).Name.ToUpper()), value); }
            }
        }

        //----- field -----

        private Dictionary<TKey, TMasterRecord> records = new Dictionary<TKey, TMasterRecord>();

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

        /// <summary> LZ4圧縮を使用するか. </summary>
        public bool UseLz4Compression { get; set; } = true;

        //----- method -----

        public void SetRecords(TMasterRecord[] masterRecords)
        {
            records.Clear();

            if (records == null) { return; }

            foreach (var masterRecord in masterRecords)
            {
                SetRecord(masterRecord);
            }
        }

        private void SetRecord(TMasterRecord masterRecord)
        {
            if (masterRecord == null) { return; }

            var key = GetRecordKey(masterRecord);

            if (records.ContainsKey(key))
            {
                var message = "Master register error!\nRegistered keys can not be registered.";
                var typeName = GetType().FullName;

                Debug.LogErrorFormat("{0}\n\n Master : {1}\nKey : {2}\n", message, typeName, key);
            }
            else
            {
                records.Add(key, masterRecord);
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
            records.Clear();
        }

        public IObservable<bool> Load(AesManaged aesManaged, bool cleanOnError)
        {
            Refresh();

            return Observable.FromMicroCoroutine<bool>(observer => LoadInternal(observer, aesManaged, cleanOnError));
        }

        private IEnumerator LoadInternal(IObserver<bool> observer, AesManaged aesManaged, bool cleanOnError)
        {
            var result = false;

            #if UNITY_EDITOR

            try
            {
                MessagePackValidater.ValidateAttribute(typeof(TMasterRecord));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            #endif

            var installPath = MasterManager.Instance.GetInstallPath<TMaster>();

            // ファイルの読み込みと復号化.
            var loadYield = Observable.FromMicroCoroutine<byte[]>(x => LoadCacheFile(x, installPath, aesManaged)).ToYieldInstruction();

            while (!loadYield.IsDone)
            {
                yield return null;
            }

            if (!loadYield.HasError && loadYield.HasResult)
            {
                var bytes = loadYield.Result;

                try
                {
                    var options = StandardResolverAllowPrivate.Options.WithResolver(UnityContractResolver.Instance);

                    if (UseLz4Compression)
                    {
                        options = options.WithCompression(MessagePackCompression.Lz4BlockArray);
                    }

                    var container = MessagePackSerializer.Deserialize<TMasterContainer>(bytes, options);

                    SetRecords(container.records);

                    result = true;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
            else
            {
                Debug.LogException(loadYield.Error);
            }

            if (!result)
            {
                if (cleanOnError)
                {
                    // 強制更新させる為バージョン情報を削除.
                    ClearVersion();
                }

                OnError();
            }

            observer.OnNext(result);
            observer.OnCompleted();
        }

        protected virtual IEnumerator LoadCacheFile(IObserver<byte[]> observer, string installPath, AesManaged aesManaged)
        {
            Func<string, AesManaged, byte[]> loadAndDecrypt = (_installPath, _aesManaged) =>
            {
                // ファイル読み込み.
                var data = File.ReadAllBytes(_installPath);

                // 復号化.               
                return data.Decrypt(_aesManaged);
            };

            // ファイルの読み込みと復号化をスレッドプールで実行.
            var loadYield = Observable.Start(() => loadAndDecrypt(installPath, aesManaged)).ObserveOnMainThread().ToYieldInstruction();

            while (!loadYield.IsDone)
            {
                yield return null;
            }

            if (!loadYield.HasError && loadYield.HasResult)
            {
                observer.OnNext(loadYield.Result);
                observer.OnCompleted();
            }
            else
            {
                observer.OnError(loadYield.Error);
            }
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

        public IEnumerable<TMasterRecord> GetAllRecords()
        {
            return records.Values;
        }

        public TMasterRecord GetRecord(TKey key)
        {
            return records.GetValueOrDefault(key);
        }

        protected virtual void OnError()
        {
            // キャッシュデータなどをクリア.
            Refresh();
        }

        /// <summary> 内部で保持しているデータをクリア. </summary>
        protected virtual void Refresh() { }

        protected abstract TKey GetRecordKey(TMasterRecord masterRecord);

        protected abstract IObservable<Unit> DownloadMaster();
    }
}
