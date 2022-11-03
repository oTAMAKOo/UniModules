
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UniRx;
using MessagePack;
using MessagePack.Resolvers;
using Extensions;
using Modules.Devkit.Console;
using Modules.MessagePack;

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

        private Subject<Unit> onUpdateMaster = null;
        private Subject<Unit> onError = null;
        private Subject<Unit> onLoadFinish = null;

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

            // 保存先設定.
            SetInstallDirectory(Application.persistentDataPath);
        }

        public void Register(IMaster master)
        {
            masters.Add(master);
        }

        public async UniTask<bool> LoadMaster(Dictionary<IMaster, string> versionTable, IProgress<float> progress = null)
        {
            Reference.Clear();

            BuildFileNameTable();

            #if UNITY_EDITOR

            if (!Prefs.checkVersion)
            {
                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "Use CachedMasterFile.");
            }

            #endif

            var result = false;
            
            var updateLog = new StringBuilder();
            var loadLog = new StringBuilder();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            if (versionTable != null)
            {
                if (progress != null) { progress.Report(0f); }

				var amount = 1f / versionTable.Count;
                var progressAmount = 0f;

                // 並行で処理.
                var tasks = new List<UniTask>();

                foreach (var element in versionTable)
                {
                    var master = element.Key;
                    var masterVersion = element.Value;

                    var masterType = master.GetType();
                    var masterName = masterType.Name;

                    var masterFileName = masterFileNames.GetValueOrDefault(masterType);

                    var localVersion = master.LoadVersion();

                    Action<bool, double> onUpdateFinish = (state, time) =>
                    {
                        if (state)
                        {
                            var localVersionText = string.IsNullOrEmpty(localVersion) ? "---" : localVersion;
                            var message = string.Format("{0} ({1:F1}ms) : {2} >> {3}", masterName, time, localVersionText, masterVersion);

                            updateLog.AppendLine(message);
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
                    };

                    Action<bool, double> onLoadFinish = (state, time) =>
                    {
                        if (state)
                        {
                            loadLog.AppendFormat("{0} ({1:F1}ms)", masterName, time).AppendLine();
                        }
                        else
                        {
                            Debug.LogErrorFormat("Load master failed.\nClass : {0}\nFile : {1}\n", masterType.FullName, masterFileName);
                        }

                        result &= state;
                        progressAmount += amount;

                        if (progress != null)
                        {
                            progress.Report(progressAmount);
                        }
                    };

                    if (master.CheckVersion(masterVersion))
                    {
                        // 読み込み.
						var task = UniTask.Defer(() => MasterLoad(master, onLoadFinish));

                        tasks.Add(task);
                    }
                    else
                    {
                        // ダウンロード + 読み込み.
						var task = UniTask.Defer(async () =>
						{
							var success = await MasterUpdate(master, masterVersion, onUpdateFinish).ToUniTask();

							if (success)
							{
								await MasterLoad(master, onLoadFinish);
							}
						});

						tasks.Add(task);
                    }
                }

                // 実行.
				try
				{
					await UniTask.WhenAll(tasks);

					result = true;
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}

				if (progress != null) { progress.Report(1f); }
			}

            stopwatch.Stop();

            if (result)
            {
                var logBuilder = new StringBuilder();

                logBuilder.AppendLine(string.Format("Master : ({0:F1}ms)", stopwatch.Elapsed.TotalMilliseconds));
                logBuilder.AppendLine();

                if (0 < updateLog.Length)
                {
                    logBuilder.AppendLine("---------- Download ----------");
                    logBuilder.AppendLine();
                    logBuilder.AppendLine(updateLog.ToString());
                }

                if (0 < loadLog.Length)
                {
                    logBuilder.AppendLine("------------ Load ------------");
                    logBuilder.AppendLine();
                    logBuilder.AppendLine(loadLog.ToString());
                }

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, logBuilder.ToString());

                if(onLoadFinish != null)
                {
                    onLoadFinish.OnNext(Unit.Default);
                }
            }
            else
            {
                if (onError != null)
                {
                    onError.OnNext(Unit.Default);
                }
            }

            return result;
        }

        private IObservable<bool> MasterUpdate(IMaster master, string masterVersion, Action<bool, double> onUpdateFinish)
        {
            Action<Exception> onErrorRetry = exception =>
            {
                var masterType = master.GetType();
                var masterName = masterType.Name;

                using (new DisableStackTraceScope())
                {
                    Debug.LogErrorFormat("{0} update retry.\n\n{1}", masterName, exception);
                }
            };

            return master.Update(masterVersion)
				.ToObservable()
                .OnErrorRetry((Exception ex)  => onErrorRetry.Invoke(ex), 3, TimeSpan.FromSeconds(5))
                .Do(x => onUpdateFinish(x.Item1, x.Item2))
                .Select(x => x.Item1);
        }

        private async UniTask<bool> MasterLoad(IMaster master, Action<bool, double> onLoadFinish)
        {
			var result = false;

            Action<Exception> onErrorRetry = exception =>
            {
                var masterType = master.GetType();
                var masterName = masterType.Name;

                using (new DisableStackTraceScope())
                {
                    Debug.LogErrorFormat("{0} load retry.\n\n{1}", masterName, exception);
                }
            };

			var loadResult = await master.Load(CryptoKey, true);

			if (onLoadFinish != null)
			{
				onLoadFinish(loadResult.Item1, loadResult.Item2);
			}

			result = loadResult.Item1;

			return result;
		}

        public void ClearMasterVersion()
        {
            masters.ForEach(x => x.ClearVersion());
            
            Reference.Clear();

            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, "Clear MasterVersion");
        }

        /// <summary> 更新が必要なマスターの数 </summary>
        public int RequireUpdateMasterCount(Dictionary<IMaster, string> versionTable)
        {
            var requireCount = 0;

            foreach (var item in versionTable)
            {
                var master = item.Key;
                var masterVersion = item.Value;

                if (!master.CheckVersion(masterVersion))
                {
                    requireCount++;
                }
            }

            return requireCount;
        }

        /// <summary> 更新が必要なマスターのファイルサイズ </summary>
        public ulong RequireUpdateMasterFileSize(Dictionary<IMaster, string> versionTable, Dictionary<IMaster, ulong> fileSizeTable)
        {
            ulong totalFileSize = 0;

            foreach (var master in masters)
            {
                var masterVersion = versionTable.GetValueOrDefault(master);

                if (!master.CheckVersion(masterVersion))
                {
                    totalFileSize += fileSizeTable.GetValueOrDefault(master);
                }
            }

            return totalFileSize;
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

        public string GetMasterFileName<T>() where T : IMaster
        {
            return GetMasterFileName(typeof(T));
        }

        public string GetMasterFileName(Type type)
        {
            if (!typeof(IMaster).IsAssignableFrom(type))
            {
                throw new Exception(string.Format("Type error require IMaster interface. : {0}", type.FullName));
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
                fileName = fileName.Encrypt(CryptoKey, true);
            }

            return fileName;
        }

        private void BuildFileNameTable()
        {
            masterFileNames = new Dictionary<Type, string>();

            foreach (var master in masters)
            {
                var type = master.GetType();

                var fileName = GetMasterFileName(type);

                if (masterFileNames.ContainsKey(type))
                {
                    var message = string.Format("File name has already been registered.\n\nClass : {0}\nFile : {1}", type.FullName, fileName);

                    throw new Exception(message);
                }

                masterFileNames.Add(type, fileName);
            }
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

            var options = StandardResolverAllowPrivate.Options.WithResolver(UnityContractResolver.Instance);

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

        public IObservable<Unit> OnErrorAsObservable()
        {
            return onError ?? (onError = new Subject<Unit>());
        }
    }
}
