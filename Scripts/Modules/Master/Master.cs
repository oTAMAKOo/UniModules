
using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
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

        IObservable<Tuple<bool, double>> Update(string masterVersion, CancellationToken cancelToken);
        IObservable<Tuple<bool, double>> Load(AesCryptoKey cryptoKey, bool cleanOnError);
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
                get { return SecurePrefs.GetString(string.Format("{0}_VERSION", typeof(TMaster).Name.ToUpper())); }
                set { SecurePrefs.SetString(string.Format("{0}_VERSION", typeof(TMaster).Name.ToUpper()), value); }
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
                    var masterManager = MasterManager.Instance;

                    instance = new TMaster();

                    masterManager.Register(instance);
                }

                return instance;
            }
        }

        /// <summary> バージョン. </summary>
        public string Version { get { return versionPrefs.version; } }

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
                var typeName = typeof(TMaster).FullName;

                var message = string.Format("Master record error!\nRecords same key already exists.\n\n Master : {0}\nKey : {1}\n", typeName, key);

                throw new InvalidDataException(message);
            }

            records.Add(key, masterRecord);
        }

        private string GetInstallPath()
        {
            var masterManager = MasterManager.Instance;

            var installDirectory = masterManager.InstallDirectory;
            var fileName = masterManager.GetMasterFileName<TMaster>();

            return PathUtility.Combine(installDirectory, fileName);
        }

        public bool CheckVersion(string masterVersion)
        {
            #if UNITY_EDITOR

            if (!MasterManager.Prefs.checkVersion) { return true; }

            #endif

            var result = true;

            var installPath = GetInstallPath();

            // ファイルがなかったらバージョン不一致.
            result &= File.Exists(installPath);

            // ローカル保存されているバージョンと一致するか.
            result &= versionPrefs.version == masterVersion;

            return result;
        }

        public void ClearVersion()
        {
            var installPath = GetInstallPath();

            if (File.Exists(installPath))
            {
                File.Delete(installPath);
            }

            versionPrefs.version = string.Empty;
            records.Clear();
        }

        public IObservable<Tuple<bool, double>> Load(AesCryptoKey cryptoKey, bool cleanOnError)
        {
            Refresh();

            return Observable.FromMicroCoroutine<Tuple<bool, double>>(observer => LoadInternal(observer, cryptoKey, cleanOnError));
        }

        private IEnumerator LoadInternal(IObserver<Tuple<bool, double>> observer, AesCryptoKey cryptoKey, bool cleanOnError)
        {
            var success = false;

            double time = 0;

            var installPath = GetInstallPath();

            // 読み込み準備.
            var prepareLoadYield = PrepareLoad(installPath).ToYieldInstruction();

            while (!prepareLoadYield.IsDone)
            {
                yield return null;
            }

            // 読み込みをスレッドプールで実行.
            var loadYield = Observable.Start(() => LoadMasterFile(installPath, cryptoKey)).ObserveOnMainThread().ToYieldInstruction(false);

            while (!loadYield.IsDone)
            {
                yield return null;
            }

            if (!loadYield.HasError && loadYield.HasResult)
            {
                success = true;
                time = loadYield.Result;
            }
            else
            {
                Debug.LogErrorFormat("Load master failed.\n\nClass : {0}\nFile : {1}\n\nException : \n{2}", typeof(TMaster).FullName, installPath, loadYield.Error);
            }

            if (!success)
            {
                if (cleanOnError)
                {
                    // 強制更新させる為バージョン情報を削除.
                    ClearVersion();
                }

                OnError();
            }

            observer.OnNext(Tuple.Create(success, time));
            observer.OnCompleted();
        }

        private double LoadMasterFile(string filePath, AesCryptoKey cryptoKey)
        {
            var masterManager = MasterManager.Instance;

            #if UNITY_EDITOR

            MessagePackValidater.ValidateAttribute(typeof(TMasterRecord));

            #endif

            var sw = System.Diagnostics.Stopwatch.StartNew();

            // ファイル読み込み.
            var bytes = FileLoad(filePath);

            // 復号化.
            if (cryptoKey != null)
            {
                bytes = Decrypt(bytes, cryptoKey);
            }

            // デシリアライズ.
            var records = Deserialize(bytes, masterManager.GetSerializerOptions());

            // レコード登録.
            SetRecords(records);

            sw.Stop();

            return sw.Elapsed.TotalMilliseconds;
        }

        protected virtual byte[] FileLoad(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            byte[] bytes = null;

            using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var memoryStream = new MemoryStream())
                {
                    fileStream.CopyTo(memoryStream);

                    bytes = memoryStream.ToArray();
                }
            }

            if (bytes == null)
            {
                throw new FileLoadException();
            }

            return bytes;
        }

        protected virtual byte[] Decrypt(byte[] bytes, AesCryptoKey cryptoKey)
        {
            if (bytes.Length == 0)
            {
                throw new InvalidDataException();
            }

            return bytes.Decrypt(cryptoKey);
        }

        protected virtual TMasterRecord[] Deserialize(byte[] bytes, MessagePackSerializerOptions options)
        {
            var container = MessagePackSerializer.Deserialize<TMasterContainer>(bytes, options);

            return container.records;
        }
        
        public IObservable<Tuple<bool, double>> Update(string masterVersion, CancellationToken cancelToken)
        {
            return Observable.FromMicroCoroutine<Tuple<bool, double>>(observer => UpdateInternal(observer, masterVersion, cancelToken));
        }

        private IEnumerator UpdateInternal(IObserver<Tuple<bool, double>> observer, string masterVersion, CancellationToken cancelToken)
        {
            var result = true;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var installPath = GetInstallPath();

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
                if (downloadYield.HasError)
                {
                    Debug.LogException(downloadYield.Error);
                }

                result = false;
            }

            // バージョン情報を更新.
            versionPrefs.version = result ? masterVersion : string.Empty;

            // ファイルが閉じるまで待つ.
            while(FileUtility.IsFileLocked(installPath))
            {
                yield return null;
            }

            sw.Stop();

            observer.OnNext(Tuple.Create(result, sw.Elapsed.TotalMilliseconds));
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

        protected virtual IEnumerator PrepareLoad(string installPath)
        {
            yield break;
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
