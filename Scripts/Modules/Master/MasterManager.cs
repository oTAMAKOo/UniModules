
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using MessagePack;
using MessagePack.Resolvers;
using Extensions;
using Modules.Devkit.Console;
using Modules.MessagePack;
using Modules.Performance;

namespace Modules.Master
{
    public sealed partial class MasterManager : Singleton<MasterManager>
    {
        //----- params -----

        public const string FolderName = "Master";

        private const string MasterFileExtension = ".master";

        private const string MasterSuffix = "Master";

        private static readonly string ConsoleEventName = "Master";

        private static readonly Color ConsoleEventColor = new Color(0.45f, 0.45f, 0.85f);

        //----- field -----

        private List<IMaster> masters = null;

        private Dictionary<Type, string> masterFileNames = null;

        private MessagePackSerializerOptions serializerOptions = null;

        private bool lz4Compression = true;
		
		private CancellationTokenSource cancelSource = null;

		private Subject<Unit> onUpdateMaster = null;
        private Subject<Unit> onLoadFinish = null;
		private Subject<Exception> onError = null;

		//----- property -----

		public IReadOnlyCollection<IMaster> Masters
        {
            get { return masters; }
        }

        /// <summary> ダウンロード先URL. </summary>
        public string DownloadUrl { get; private set; }

        /// <summary> 保存先. </summary>
        public string InstallDirectory { get; private set; }

        /// <summary> 暗号化キー. </summary>
        public AesCryptoKey CryptoKey { get; private set; }

        /// <summary> LZ4圧縮を使用するか. </summary>
        public bool Lz4Compression
        {
            get { return lz4Compression; }
            set
            {
                lz4Compression = value;
                serializerOptions = null;
            }
        }

        //----- method -----

        private MasterManager()
        {
            masters = new List<IMaster>();
            masterFileNames = new Dictionary<Type, string>();

			cancelSource = new CancellationTokenSource();

			InitializeVersion();

			// 保存先設定.
			SetInstallDirectory(Application.persistentDataPath);
        }

        public void Register(IMaster master)
        {
            var type = master.GetType();

            if (!typeof(IMaster).IsAssignableFrom(type))
            {
                throw new Exception($"Type error require IMaster interface. : {type.FullName}");
            }

            if (masters.Contains(master)){ return; }

            masters.Add(master);
        }

        public async UniTask<bool> UpdateMaster(Dictionary<IMaster, string> updateMasters, IProgress<float> progress = null, CancellationToken cancelToken = default)
        {
            var result = true;

			var linkedCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, cancelSource.Token);

			var linkedCancelToken = linkedCancelTokenSource.Token;

			var tasks = new List<UniTask>();

            var updateLog = new StringBuilder();

            Reference.Clear();

            #if UNITY_EDITOR

            if (!EnableVersionCheck)
            {
                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "Use CachedMasterFile.");

                return true;
            }

            #endif

            var amount = 1f / updateMasters.Count;
            var progressAmount = 0f;

            void OnUpdateFinish(Type masterType, string masterName, string masterFileName, bool state, double time)
            {
                if (state)
                {
                    var message = $"{masterName} ({time:F1}ms)";

                    lock (updateLog)
                    {
                        updateLog.AppendLine(message);
                    }
                }
                else
                {
                    Debug.LogErrorFormat("Update master failed.\nClass : {0}\nFile : {1}\n", masterType.FullName, masterFileName);
                }

                result &= state;
                progressAmount += amount;

                if (progress != null)
                {
                    progress.Report(progressAmount);
                }

                if (onUpdateMaster != null)
                {
                    onUpdateMaster.OnNext(Unit.Default);
                }
            }

			Exception exception = null;

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            if (progress != null) { progress.Report(0f); }

			var frameCallLimiter = new FunctionFrameLimiter(50);

            if (updateMasters.Any())
            {
				// 実行.
				try
				{
					foreach (var element in updateMasters)
					{
						var master = element.Key;
						var masterType = master.GetType();
						var masterName = masterType.Name;
						var masterVersion = element.Value;
						var masterFileName = masterFileNames.GetValueOrDefault(masterType);

						var task = UniTask.Defer(async () =>
						{
							var updateResult = await master.Update(masterVersion, frameCallLimiter, linkedCancelToken);

							if (linkedCancelToken.IsCancellationRequested){ return; }

							OnUpdateFinish(masterType, masterName, masterFileName, updateResult.Item1, updateResult.Item2);

							var success = updateResult.Item1;

							if (success)
							{
								UpdateVersion(master, masterVersion);
							}
							else
							{
								throw new Exception($"Failed master update. {masterName}");
							}
						});

						tasks.Add(task);
					}

					await UniTask.WhenAll(tasks);

					await SaveVersion();
				}
				catch (OperationCanceledException)
				{
					/* Canceled */
				}
				catch (Exception e)
				{
					exception = e;
				}
            }

            stopwatch.Stop();

            if (result)
            {
				var title = $"Master Update : ({stopwatch.Elapsed.TotalMilliseconds:F1}ms) ";

				void OutputCallback(string x)
				{
					UnityConsole.Event(ConsoleEventName, ConsoleEventColor, x);
				}

				LogUtility.ChunkLog(updateLog.ToString(), title, OutputCallback);

				if (progress != null) { progress.Report(1f); }
            }
            else
            {
                if (onError != null)
                {
                    onError.OnNext(exception);
                }
				else
				{
					Debug.LogException(exception);
				}
            }

            return result;
        }

        public async UniTask<bool> LoadMaster(CancellationToken cancelToken = default)
        {
			Reference.Clear();

			Exception exception = null;

			var result = true;

			var linkedCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, cancelSource.Token);

			var linkedCancelToken = linkedCancelTokenSource.Token;

			var loadLog = new StringBuilder();

            void OnLoadFinish(Type masterType, string masterName, string masterFileName, bool state, double prepareTime, double loadTime)
            {
                if (state)
                {
                    lock (loadLog)
                    {
						loadLog.AppendFormat("{0} (prepare : {1:F1}ms, load : {2:F1}ms)", masterName, prepareTime, loadTime).AppendLine();
					}
                }
                else
                {
                    Debug.LogErrorFormat("Load master failed.\nClass : {0}\nFile : {1}\n", masterType.FullName, masterFileName);
                }

                result &= state;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			try
			{
				var chunk = masters.Chunk(50);

				foreach (var items in chunk)
				{
					var tasks = new List<UniTask>();

					foreach (var item in items)
					{
						var masterType = item.GetType();
						var masterName = masterType.Name;

						var masterFileName = masterFileNames.GetValueOrDefault(masterType);

						var task = UniTask.Defer(async () =>
						{
							var loadResult = await item.Load(CryptoKey, true, linkedCancelToken);

							if (linkedCancelToken.IsCancellationRequested) { return; }

							OnLoadFinish(masterType, masterName, masterFileName, loadResult.Item1, loadResult.Item2, loadResult.Item3);
						});

						tasks.Add(task);
					}

					await UniTask.WhenAll(tasks);
				}
			}
			catch (Exception e)
			{
				exception = e;
			}

			stopwatch.Stop();

            if (result)
            {
				var title = $"Master Load : ({stopwatch.Elapsed.TotalMilliseconds:F1}ms) ";

				void OutputCallback(string x)
				{
					UnityConsole.Event(ConsoleEventName, ConsoleEventColor, x);
				}

				LogUtility.ChunkLog(loadLog.ToString(), title, OutputCallback);

				if (onLoadFinish != null)
                {
                    onLoadFinish.OnNext(Unit.Default);
                }
            }
            else
            {
                if (onError != null)
                {
                    onError.OnNext(exception);
                }
				else
				{
					Debug.LogException(exception);
				}
            }

            return result;
        }

		public void CancelAll()
		{
			if (cancelSource != null)
			{
				cancelSource.Cancel();

				// キャンセルしたので再生成.
				cancelSource = new CancellationTokenSource();
			}
		}

		public void ClearMasterVersion()
        {
			DeleteVersionFile();
            
            Reference.Clear();

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "Clear MasterVersion");
        }

        /// <summary> 更新が必要なマスター </summary>
        public async UniTask<IMaster[]> RequireUpdateMasters(Dictionary<IMaster, string> versionTable)
        {
            var list = new List<IMaster>();

            #if UNITY_EDITOR
                    
            var enableVersionCheck = EnableVersionCheck;

            #endif

			var tasks = new List<UniTask>();

			try
			{
				foreach (var item in masters)
		        {
	                var master = item;

		            var task = UniTask.RunOnThreadPool(() =>
		            {
		                var masterVersion = versionTable.GetValueOrDefault(master);

		                var versionCheck = CheckVersion(master, masterVersion);

		                #if UNITY_EDITOR
		                    
		                versionCheck = !enableVersionCheck || versionCheck;

		                #endif

		                if (!versionCheck)
		                {
		                    lock (list)
		                    {
		                        list.Add(master);
		                    }
		                }
		            });

		            tasks.Add(task);
		        }

		        await UniTask.WhenAll(tasks);
			}
			finally
			{
				await UniTask.SwitchToMainThread();
			}

			return list.ToArray();
        }

        public void Clear()
        {
            masters.Clear();
        }

        public void SetDownloadUrl(string downloadUrl)
        {
            DownloadUrl = downloadUrl;
        }

        public void SetInstallDirectory(string installDirectory)
        {
            InstallDirectory = PathUtility.Combine(installDirectory, FolderName);

            #if UNITY_IOS

            if (InstallDirectory.StartsWith(Application.persistentDataPath))
            {
                UnityEngine.iOS.Device.SetNoBackupFlag(InstallDirectory);
            }

            #endif
        }

        /// <summary> 暗号化キー設定 </summary>
        public void SetCryptoKey(AesCryptoKey cryptoKey)
        {
            CryptoKey = cryptoKey;
        }

		public string GetFilePath(IMaster master)
		{
			var fileName = GetMasterFileName(master.GetType());

			return PathUtility.Combine(InstallDirectory, fileName);
		}

		public string GetMasterFileName<T>() where T : IMaster
        {
            return GetMasterFileName(typeof(T));
        }

        public string GetMasterFileName(Type type)
        {
			if (type == null){ return null; }

            if (masterFileNames.ContainsKey(type))
            {
                return masterFileNames[type];
            }

            var fileName = string.Empty;

            // FileNameAttributeを持っている場合はそちらの名前を採用する.

            var fileNameAttribute = type.GetCustomAttributes(typeof(FileNameAttribute), false)
                .Cast<FileNameAttribute>()
                .FirstOrDefault();

            if (fileNameAttribute != null)
            {
                fileName = fileNameAttribute.FileName;
            }
            else
            {
                // クラス名をファイル名として採用.
                fileName = DeleteMasterSuffix(type.Name);
            }

            fileName = Path.ChangeExtension(fileName, MasterFileExtension);

            // 暗号化オブジェクトが設定されていたら暗号化.

            if (CryptoKey != null)
            {
				lock (CryptoKey)
				{
					fileName = fileName.Encrypt(CryptoKey, true);
				}
            }

            // 登録.

			lock (masterFileNames)
			{
				if (!masterFileNames.ContainsKey(type))
				{
					masterFileNames.Add(type, fileName);
				}
			}

			return fileName;
        }

        public static string DeleteMasterSuffix(string fileName)
        {
            // 末尾が「Master」だったら末尾を削る.
            if (fileName.EndsWith(MasterSuffix))
            {
                fileName = fileName.SafeSubstring(0, fileName.Length - MasterSuffix.Length);
            }

            return fileName;
        }

		public MessagePackSerializerOptions GetSerializerOptions()
        {
            if (serializerOptions != null) { return serializerOptions; }

            var options = StandardResolverAllowPrivate.Options.WithResolver(UnityCustomResolver.Instance);

            if (Lz4Compression)
            {
                options = options.WithCompression(MessagePackCompression.Lz4BlockArray);
            }

            serializerOptions = options;

            return serializerOptions;
        }

        public IObservable<Unit> OnLoadFinishAsObservable()
        {
            return onLoadFinish ?? (onLoadFinish = new Subject<Unit>());
        }

        public IObservable<Unit> OnUpdateMasterAsObservable()
        {
            return onUpdateMaster ?? (onUpdateMaster = new Subject<Unit>());
        }

        public IObservable<Exception> OnErrorAsObservable()
        {
            return onError ?? (onError = new Subject<Exception>());
        }
    }
}
