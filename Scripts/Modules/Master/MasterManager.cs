
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

            var type = master.GetType();

            var fileName = GetMasterFileName(type);

            if (masterFileNames.ContainsKey(type))
            {
                var message = string.Format("File name has already been registered.\n\nClass : {0}\nFile : {1}", type.FullName, fileName);

                throw new Exception(message);
            }

            masterFileNames.Add(type, fileName);
        }

        public async UniTask<bool> UpdateMaster(Dictionary<IMaster, string> updateMasters, IProgress<float> progress = null)
        {
            var result = true;

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
                    var message = string.Format("{0} ({1:F1}ms)", masterName, time);

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

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            if (progress != null) { progress.Report(0f); }

            foreach (var element in updateMasters)
            {
                var master = element.Key;
                var masterType = master.GetType();
                var masterName = masterType.Name;
                var masterVersion = element.Value;
                var masterFileName = masterFileNames.GetValueOrDefault(masterType);

                Action<bool, double> onUpdateFinishCallback = (state, time) =>
                {
                    OnUpdateFinish(masterType, masterName, masterFileName, state, time);
                };

                var task = UniTask.Defer(async () =>
				{
					var success = await MasterUpdate(master, masterVersion, onUpdateFinishCallback).ToUniTask();

                    if (!success)
                    {
                        throw new Exception($"Failed master update. {masterName}");
                    }
                });

				tasks.Add(task);
            }

            // 実行.
			try
			{
                if (tasks.Any())
                {
    				await UniTask.WhenAll(tasks);
                }
			}
			catch (Exception e)
			{
				Debug.LogException(e);
            }

            stopwatch.Stop();

            if (result)
            {
                var logBuilder = new StringBuilder();

                logBuilder.AppendLine(string.Format("Master Update : ({0:F1}ms)", stopwatch.Elapsed.TotalMilliseconds));

                if (0 < updateLog.Length)
                {   
                    logBuilder.AppendLine();
                    logBuilder.AppendLine(updateLog.ToString());
                }

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, logBuilder.ToString());

                if (progress != null) { progress.Report(1f); }
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

        public async UniTask<bool> LoadMaster()
        {
            var result = true;

            var tasks = new List<UniTask>();

            Reference.Clear();
            
            var loadLog = new StringBuilder();

            void OnLoadFinish(Type masterType, string masterName, string masterFileName, bool state, double time)
            {
                if (state)
                {
                    lock (loadLog)
                    {
                        loadLog.AppendFormat("{0} ({1:F1}ms)", masterName, time).AppendLine();
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
                foreach (var master in masters)
                {
                    var masterType = master.GetType();
                    var masterName = masterType.Name;

                    var masterFileName = masterFileNames.GetValueOrDefault(masterType);
                
                    Action<bool, double> onLoadFinishCallback = (state, time) => OnLoadFinish(masterType, masterName, masterFileName, state, time);

                    var task = UniTask.RunOnThreadPool(async () => { return await MasterLoad(master, onLoadFinishCallback); });

                    tasks.Add(task);
                }

				await UniTask.WhenAll(tasks);
            }
			catch (Exception e)
			{
				Debug.LogException(e);
            }
            
            stopwatch.Stop();

            if (result)
            {
                var logBuilder = new StringBuilder();

                logBuilder.AppendLine(string.Format("Master Load : ({0:F1}ms)", stopwatch.Elapsed.TotalMilliseconds));

                if (0 < loadLog.Length)
                {
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
			var loadResult = await master.Load(CryptoKey, true);

			if (onLoadFinish != null)
			{
				onLoadFinish(loadResult.Item1, loadResult.Item2);
			}

			return loadResult.Item1;
		}

        public void ClearMasterVersion()
        {
            masters.ForEach(x => x.ClearVersion());
            
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
            
            async UniTask CheckRequireUpdate(IEnumerable<IMaster> masters)
            {
                foreach (var master in masters)
                {
                    var masterVersion = versionTable.GetValueOrDefault(master);

                    var versionCheck = await master.CheckVersion(masterVersion);

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
                }
            }

            var tasks = new List<UniTask>();

            var chunk = masters.Chunk(50);

            foreach (var items in chunk)
            {
                var masters = items;

                var task = UniTask.RunOnThreadPool(async () =>
                {
                    await CheckRequireUpdate(masters);
                });

                tasks.Add(task);
            }

            await UniTask.WhenAll(tasks);

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

        public IObservable<Unit> OnErrorAsObservable()
        {
            return onError ?? (onError = new Subject<Unit>());
        }
    }
}
