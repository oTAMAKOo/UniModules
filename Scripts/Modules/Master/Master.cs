
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using MessagePack;
using Modules.Performance;

namespace Modules.Master
{
    public interface IMaster
    {
		string LoadVersion();
        
        bool CheckVersion(string masterVersion, string localVersion);
        bool CheckVersion(string masterVersion);
        
        void ClearVersion();

        UniTask<Tuple<bool, double>> Update(string masterVersion, FunctionFrameLimiter frameCallLimiter);
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

        private async UniTask UpdateVersion(string newVersion)
        {
            // バージョン更新.

			var versionFilePath = string.Empty;

			try
            {
				var filePath = GetFilePath();

				versionFilePath = Path.ChangeExtension(filePath, VersionFileExtension);

				var bytes = Encoding.UTF8.GetBytes(newVersion);
                
				using (var fs = new FileStream(versionFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 64, true))
				{
					await fs.WriteAsync(bytes, 0, bytes.Length);
				}
			}
            catch
            {
                if (File.Exists(versionFilePath))
                {
                    File.Delete(versionFilePath);
                }
            }
        }

        public bool CheckVersion(string masterVersion, string localVersion)
        {
            var result = true;

            var filePath = GetFilePath();

            // ファイルがなかったらバージョン不一致.
            result &= File.Exists(filePath);

            // ローカル保存されているバージョンと一致するか.
            result &= localVersion == masterVersion;

            return result;
        }

        public bool CheckVersion(string masterVersion)
        {
            var localVersion = LoadVersion();

            return CheckVersion(masterVersion, localVersion);
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
            var filePath = string.Empty;

            var success = false;

            double time = 0;


			try
			{
                var sw = System.Diagnostics.Stopwatch.StartNew();

                await UniTask.SwitchToThreadPool();

                filePath = GetFilePath();

                Refresh();

                await UniTask.SwitchToMainThread();

                // 読み込み準備.
                await PrepareLoad(filePath);

                await UniTask.SwitchToThreadPool();

                // 読み込み.
                LoadMasterFile(filePath, cryptoKey);

                await UniTask.SwitchToMainThread();

                sw.Stop();

                time = sw.Elapsed.TotalMilliseconds;

                success = true;
			}
			catch (Exception e)
			{
				Debug.LogErrorFormat("Load master failed.\n\nClass : {0}\nFile : {1}\n\nException : \n{2}", typeof(TMaster).FullName, filePath, e);
			}
            finally
            {
                await UniTask.SwitchToMainThread();
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

		private void LoadMasterFile(string filePath, AesCryptoKey cryptoKey)
		{
			var masterManager = MasterManager.Instance;

            var serializerOptions = masterManager.GetSerializerOptions();
                
            // ファイル読み込み.
            var bytes = FileLoad(filePath);

            // 復号化.
            if (cryptoKey != null)
            {
                // AesCryptoKeyはスレッドセーフではないので専用キーを作成.
                var threadCryptoKey = new AesCryptoKey(cryptoKey.Key, cryptoKey.Iv);

                bytes = Decrypt(bytes, threadCryptoKey);
            }

            // MessagePackの不具合対応.
            // ※ コード生成をDeserialize時に実行すると処理が返ってこないのでここで生成.

            #if UNITY_EDITOR

            serializerOptions.Resolver.GetFormatter<TMaster>();

            #endif

            // デシリアライズ.
            var records = Deserialize(bytes, serializerOptions);

            // レコード登録.
            SetRecords(records);
        }

        protected virtual byte[] FileLoad(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            var bytes = File.ReadAllBytes(filePath);

            if (bytes == null)
            {
                throw new FileLoadException();
            }

            return bytes;
        }

        protected virtual byte[] Decrypt(byte[] bytes, AesCryptoKey cryptoKey)
        {
            if (bytes.Length == 0){ return new byte[0]; }

            byte[] result;

            lock (cryptoKey)
            {
                result = bytes.Decrypt(cryptoKey);
            }

            return result;
        }

        protected virtual TMasterRecord[] Deserialize(byte[] bytes, MessagePackSerializerOptions options)
        {
            var container = MessagePackSerializer.Deserialize<TMasterContainer>(bytes, options);

            return container != null ? container.records : new TMasterRecord[0];
        }
        
		public async UniTask<Tuple<bool, double>> Update(string masterVersion, FunctionFrameLimiter frameCallLimiter)
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
                await UniTask.SwitchToMainThread();

                await frameCallLimiter.Wait();

                await DownloadMaster(masterVersion);

				if (File.Exists(filePath))
				{
					result = true;
				}

                // ファイルが閉じるまで待つ.
                while(FileUtility.IsFileLocked(filePath))
                {
                    await UniTask.NextFrame();
                }

                await UniTask.SwitchToThreadPool();

                // バージョン情報を更新.
                var version = result ? masterVersion : string.Empty;
            
                await UpdateVersion(version);
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
            finally
            {
                await UniTask.SwitchToMainThread();
            }

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

        protected abstract UniTask DownloadMaster(string version);
    }
}
