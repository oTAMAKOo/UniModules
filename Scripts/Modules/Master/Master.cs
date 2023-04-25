
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Extensions;
using MessagePack;
using Modules.Performance;

namespace Modules.Master
{
	public interface IMaster
	{
		UniTask<Tuple<bool, double>> Update(string masterVersion, FunctionFrameLimiter frameCallLimiter, CancellationToken cancelToken = default);
		UniTask<Tuple<bool, double, double>> Load(AesCryptoKey cryptoKey, bool cleanOnError, CancellationToken cancelToken = default);
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

				var message = $"Master record error!\nRecords same key already exists.\n\n Master : {typeName}\nKey : {key}\n";

				throw new Exception(message);
			}

			records.Add(key, masterRecord);
		}

		public async UniTask<Tuple<bool, double, double>> Load(AesCryptoKey cryptoKey, bool cleanOnError, CancellationToken cancelToken = default)
		{
			var masterManager = MasterManager.Instance;

			var filePath = string.Empty;

			var success = false;

			var prepareTime = 0D;
			var loadTime = 0D;

			try
			{
				filePath = masterManager.GetFilePath(this);

				Refresh();

				// 読み込み準備.
				prepareTime = await PrepareLoadMasterFile(filePath, cancelToken);

				// 読み込み.
				loadTime = await UniTask.RunOnThreadPool(() => LoadMasterFile(filePath, cryptoKey), cancellationToken: cancelToken);

				success = true;
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
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
					if (File.Exists(filePath))
					{
						File.Delete(filePath);
					}

					// 強制更新させる為バージョン情報を削除.
					await masterManager.ClearVersion(this);
				}

				OnError();
			}

			return Tuple.Create(success, prepareTime, loadTime);
		}

		private async UniTask<double> PrepareLoadMasterFile(string filePath, CancellationToken cancelToken)
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

			await PrepareLoad(filePath, cancelToken);

			sw.Stop();

			var time = sw.Elapsed.TotalMilliseconds;

			return time;
		}

		private double LoadMasterFile(string filePath, AesCryptoKey cryptoKey)
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();

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

			sw.Stop();

			var time = sw.Elapsed.TotalMilliseconds;

			return time;
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
			if (bytes.Length == 0) { return new byte[0]; }

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

		public async UniTask<Tuple<bool, double>> Update(string masterVersion, FunctionFrameLimiter frameCallLimiter, CancellationToken cancelToken = default)
		{
			var masterManager = MasterManager.Instance;

			var result = false;

			var sw = System.Diagnostics.Stopwatch.StartNew();

			var filePath = masterManager.GetFilePath(this);

			try
			{
				await frameCallLimiter.Wait(cancelToken: cancelToken);

				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}

				result = await Download(masterVersion, cancelToken);

				if (File.Exists(filePath))
				{
					// ファイルが閉じるまで待つ.
					while (FileUtility.IsFileLocked(filePath))
					{
						await UniTask.NextFrame(cancelToken);
					}
				}
				else
				{
					result = false;
				}
			}
			catch (OperationCanceledException)
			{
				/* Canceled */
			}
			catch (Exception e)
			{
				Debug.LogException(e);
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

		protected virtual UniTask PrepareLoad(string installPath, CancellationToken cancelToken)
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

		protected abstract UniTask<bool> Download(string version, CancellationToken cancelToken);
	}
}
