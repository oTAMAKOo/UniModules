
using UnityEngine;
using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UniRx;
using Extensions;
using MessagePack;

namespace Modules.Master
{
    public interface IMaster
    {
        string LoadVersion();
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

        private const string VersionFileExtension = ".version";

        //----- field -----
        
        private Dictionary<TKey, TMasterRecord> records = new Dictionary<TKey, TMasterRecord>();

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

        //----- method -----

        public void SetRecords(TMasterRecord[] masterRecords)
        {
            records.Clear();

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

                throw new Exception(message);
            }

            records.Add(key, masterRecord);
        }

        private string GetFilePath()
        {
            var masterManager = MasterManager.Instance;

            var installDirectory = masterManager.InstallDirectory;
            var fileName = masterManager.GetMasterFileName<TMaster>();

            return PathUtility.Combine(installDirectory, fileName);
        }

        public string LoadVersion()
        {
            var filePath = GetFilePath();

            var versionFilePath = Path.ChangeExtension(filePath, VersionFileExtension);

            var version = string.Empty;

            try
            {
                if (File.Exists(versionFilePath))
                {
                    var bytes = File.ReadAllBytes(versionFilePath);

                    version = Encoding.UTF8.GetString(bytes);
                }
            }
            catch
            {
                version = null;

                if (File.Exists(versionFilePath))
                {
                    File.Delete(versionFilePath);
                }
            }

            return version;
        }

        private void UpdateVersion(string newVersion)
        {
            var filePath = GetFilePath();

            var versionFilePath = Path.ChangeExtension(filePath, VersionFileExtension);

            try
            {
                var bytes = Encoding.UTF8.GetBytes(newVersion);

                File.WriteAllBytes(versionFilePath, bytes);
            }
            catch
            {
                if (File.Exists(versionFilePath))
                {
                    File.Delete(versionFilePath);
                }
            }
        }

        public bool CheckVersion(string masterVersion)
        {
            #if UNITY_EDITOR

            if (!MasterManager.Prefs.checkVersion) { return true; }

            #endif

            var result = true;

            var version = LoadVersion();

            var filePath = GetFilePath();

            // ファイルがなかったらバージョン不一致.
            result &= File.Exists(filePath);

            // ローカル保存されているバージョンと一致するか.
            result &= version == masterVersion;

            return result;
        }

        public void ClearVersion()
        {
            var filePath = GetFilePath();

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            var versionFilePath = Path.ChangeExtension(filePath, VersionFileExtension);

            if (File.Exists(versionFilePath))
            {
                File.Delete(versionFilePath);
            }
            
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

            var filePath = GetFilePath();

            // 読み込み準備.
            var prepareLoadYield = PrepareLoad(filePath).ToYieldInstruction();

            while (!prepareLoadYield.IsDone)
            {
                yield return null;
            }

            // 読み込みをスレッドプールで実行.
            var loadYield = Observable.Start(() => LoadMasterFile(filePath, cryptoKey)).ObserveOnMainThread().ToYieldInstruction(false);

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
                Debug.LogErrorFormat("Load master failed.\n\nClass : {0}\nFile : {1}\n\nException : \n{2}", typeof(TMaster).FullName, filePath, loadYield.Error);
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
            if (bytes.Length == 0){ return new byte[0]; }

            return bytes.Decrypt(cryptoKey);
        }

        protected virtual TMasterRecord[] Deserialize(byte[] bytes, MessagePackSerializerOptions options)
        {
            var container = MessagePackSerializer.Deserialize<TMasterContainer>(bytes, options);

            return container != null ? container.records : new TMasterRecord[0];
        }
        
        public IObservable<Tuple<bool, double>> Update(string masterVersion, CancellationToken cancelToken)
        {
            return Observable.FromMicroCoroutine<Tuple<bool, double>>(observer => UpdateInternal(observer, masterVersion, cancelToken));
        }

        private IEnumerator UpdateInternal(IObserver<Tuple<bool, double>> observer, string masterVersion, CancellationToken cancelToken)
        {
            var result = true;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var filePath = GetFilePath();

            // 既存のファイル削除.
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // ダウンロード.
            var downloadYield = DownloadMaster().ToYieldInstruction(false, cancelToken);

            while (!downloadYield.IsDone)
            {
                yield return null;
            }

            if (!downloadYield.HasResult || downloadYield.HasError || !File.Exists(filePath))
            {
                if (downloadYield.HasError)
                {
                    Debug.LogException(downloadYield.Error);
                }

                result = false;
            }

            // ファイルが閉じるまで待つ.
            while(FileUtility.IsFileLocked(filePath))
            {
                yield return null;
            }

            // バージョン情報を更新.
            var version = result ? masterVersion : string.Empty;
            
            UpdateVersion(version);

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
