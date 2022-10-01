
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using MessagePack;

namespace Modules.Master
{
    public interface IMaster
    {
        string LoadVersion();
        bool CheckVersion(string masterVersion);
        void ClearVersion();

        UniTask<Tuple<bool, double>> Update(string masterVersion);
		UniTask<Tuple<bool, double>> Load(AesCryptoKey cryptoKey, bool cleanOnError);
    }

    public abstract class MasterContainer<TMasterRecord>
    {
        public TMasterRecord[] records = null;
    }

    public abstract class Master<TKey, TMaster, TMasterContainer, TMasterRecord> : IMaster
        where TKey : IComparable
        where TMaster : Master<TKey, TMaster, TMasterContainer, TMasterRecord>, new()
        where TMasterContainer : MasterContainer<TMasterRecord>, new()
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

        public async UniTask<Tuple<bool, double>> Load(AesCryptoKey cryptoKey, bool cleanOnError)
        {
            Refresh();

			var success = false;

            double time = 0;

            var filePath = GetFilePath();

            // 読み込み準備.
            await PrepareLoad(filePath);

			// 読み込みをスレッドプールで実行.
			try
			{
				time = await UniTask.RunOnThreadPool(() => LoadMasterFile(filePath, cryptoKey));

				success = true;
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("Load master failed.\n\nClass : {0}\nFile : {1}\n\nException : \n{2}", typeof(TMaster).FullName, filePath, e);
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
			
			return Tuple.Create(success, time);
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
        
        public async UniTask<Tuple<bool, double>> Update(string masterVersion)
        {
			var result = false;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var filePath = GetFilePath();

            // 既存のファイル削除.
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // ダウンロード.
			try
			{
				await DownloadMaster();

				if (File.Exists(filePath))
				{
					result = true;
				}
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}

			// ファイルが閉じるまで待つ.
            while(FileUtility.IsFileLocked(filePath))
            {
                await UniTask.NextFrame();
            }

            // バージョン情報を更新.
            var version = result ? masterVersion : string.Empty;
            
            UpdateVersion(version);

            sw.Stop();
			
			return Tuple.Create(result, sw.Elapsed.TotalMilliseconds);
        }

        public IEnumerable<TMasterRecord> GetAllRecords()
        {
            return records.Values;
        }

        public TMasterRecord GetRecord(TKey key)
        {
            return records.GetValueOrDefault(key);
        }

        protected virtual UniTask PrepareLoad(string installPath)
        {
            return UniTask.CompletedTask;
        }

        protected virtual void OnError()
        {
            // キャッシュデータなどをクリア.
            Refresh();
        }

        /// <summary> 内部で保持しているデータをクリア. </summary>
        protected virtual void Refresh() { }

        protected abstract TKey GetRecordKey(TMasterRecord masterRecord);

        protected abstract UniTask DownloadMaster();
    }
}
