
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

        public static readonly string ConsoleEventName = "Master";

        public static readonly Color ConsoleEventColor = new Color(0.45f, 0.45f, 0.85f);

        public const string FolderName = "Master";

        private const string MasterFileExtension = ".master";

        private const string MasterSuffix = "Master";

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
            SetInstallDirectory(UnityPathUtility.PersistentDataPath);
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

        public void Remove(IMaster master)
        {
            if (!masters.Contains(master)){ return; }

            masters.Remove(master);
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

            if (!EnableVersionCheck) { return true; }

            #endif

            var amount = 1f / updateMasters.Count;
            var progressAmount = 0f;

            var frameCallLimiter = new FunctionFrameLimiter(50);

            async UniTask MasterUpdate(IMaster master, string masterVersion)
            {
                var masterType = master.GetType();
                var masterName = masterType.Name;
                var masterFileName = masterFileNames.GetValueOrDefault(masterType);

                var updateResult = await master.Update(masterVersion, frameCallLimiter, linkedCancelToken);

                if (linkedCancelToken.IsCancellationRequested){ return; }

                var success = updateResult.Item1;
                var time = updateResult.Item2;

                if (success)
                {
                    UpdateVersion(master, masterVersion);

                    var message = $"{masterName} ({time:F1}ms)";

                    lock (updateLog)
                    {
                        updateLog.AppendLine(message);
                    }
                }
                else
                {
                    throw new Exception($"Update master failed.\nClass : {masterType.FullName}\nFile : {masterFileName}");
                }

                result &= success;
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

            if (updateMasters.Any())
            {
                // 実行.
                try
                {
                    var chunk = updateMasters.Chunk(25);

                    foreach (var items in chunk)
                    {
                        foreach (var item in items)
                        {
                            var task = MasterUpdate(item.Key, item.Value);

                            tasks.Add(task);
                        }

                        await UniTask.NextFrame(CancellationToken.None);
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
                var title = $"Master Update : ({stopwatch.Elapsed.TotalMilliseconds:F1}ms)";

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
            var resultLockObject = new System.Object();

            var linkedCancelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancelToken, cancelSource.Token);

            var linkedCancelToken = linkedCancelTokenSource.Token;

            var prepareTimes = new Dictionary<IMaster, double>();
            var loadTimes = new Dictionary<IMaster, double>();
            var setupTimes = new Dictionary<IMaster, double>();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                //-------------------------------------------------------------
                // Editorの場合、動的コード生成が実行される為負荷が高くなる.
                // 実機では事前コード生成の為負荷はそこまで高くならない.
                //-------------------------------------------------------------

                //------ Prepare ------

                if (result)
                {
                    var tasks = new List<UniTask>();

                    foreach (var item in masters)
                    {
                        var master = item;

                        var task = UniTask.Defer(async () =>
                        { 
                            var prepareResult = await master.Prepare(linkedCancelToken);

                            lock (resultLockObject)
                            {
                                result &= prepareResult.Item1;
                            }

                            if (prepareResult.Item1)
                            {
                                prepareTimes.Add(master, prepareResult.Item2);

                                if (InstallDirectory.StartsWith(UnityPathUtility.StreamingAssetsPath))
                                {
                                    await ClearVersion(master);
                                }
                            }
                            else
                            {
                                var masterType = master.GetType();
                                var masterFileName = masterFileNames.GetValueOrDefault(masterType);

                                Debug.LogErrorFormat("Prepare master failed.\nClass : {0}\nFile : {1}\n", masterType.FullName, masterFileName);
                            }
                        });

                        tasks.Add(task);
                    }

                    await UniTask.WhenAll(tasks);
                }

                //------ Load ------

                if (result)
                {
                    await UniTask.RunOnThreadPool(async () =>
                    {
                        var tasks = new List<UniTask>();

                        foreach (var item in masters)
                        {
                            var master = item;

                            var task = UniTask.Defer(async () =>
                            {
                                var loadResult = await master.Load(CryptoKey, true, false, linkedCancelToken);

                                lock (resultLockObject)
                                {
                                    result &= loadResult.Item1;
                                }

                                if (loadResult.Item1)
                                {
                                    lock (loadTimes)
                                    {
                                        loadTimes.Add(master, loadResult.Item2);
                                    }
                                }
                                else
                                {
                                    var masterType = master.GetType();
                                    var masterFileName = masterFileNames.GetValueOrDefault(masterType);

                                    Debug.LogErrorFormat("Load master failed.\nClass : {0}\nFile : {1}\n", masterType.FullName, masterFileName);
                                }
                            });

                            tasks.Add(task);
                        }

                        await UniTask.WhenAll(tasks);

                    }, cancellationToken: linkedCancelToken);
                }

                //------ Setup ------

                if (result)
                {
                    await UniTask.RunOnThreadPool(async () =>
                    {
                        var tasks = new List<UniTask>();

                        foreach (var item in masters)
                        {
                            var master = item;

                            var task = UniTask.RunOnThreadPool(() =>
                            {
                                var setupResult = master.Setup();
                                
                                if (setupResult.Item1)
                                {
                                    lock (resultLockObject)
                                    {
                                        result &= setupResult.Item1;
                                    }

                                    lock (setupTimes)
                                    {
                                        setupTimes.Add(master, setupResult.Item2);
                                    }
                                }
                                else
                                {
                                    var masterType = master.GetType();
                                    var masterFileName = masterFileNames.GetValueOrDefault(masterType);

                                    Debug.LogErrorFormat("Setup master failed.\nClass : {0}\nFile : {1}\n", masterType.FullName, masterFileName);
                                }
                                
                            }, false, linkedCancelToken);

                            tasks.Add(task);
                        }

                        await UniTask.WhenAll(tasks);

                    }, cancellationToken: linkedCancelToken);
                }
            }
            catch (Exception e)
            {
                exception = e;
            }

            stopwatch.Stop();

            if (result)
            {
                var logBuilder = new StringBuilder();

                foreach (var master in masters)
                {
                    var masterType = master.GetType();
                    var masterName = masterType.Name;

                    var prepareTime = prepareTimes.GetValueOrDefault(master);
                    var loadTime = loadTimes.GetValueOrDefault(master);
                    var setupTime = setupTimes.GetValueOrDefault(master);

                    var table = new Tuple<double, string>[]
                    {
                        Tuple.Create(prepareTime, $"prepare : {prepareTime:F1}ms"),
                        Tuple.Create(prepareTime, $"load : {loadTime:F1}ms"),
                        Tuple.Create(prepareTime, $"setup : {setupTime:F1}ms"),
                    };

                    logBuilder.Append($"{masterName} (");

                    for (var i = 0; i < table.Length; i++)
                    {
                        if (0 < i)
                        {
                            logBuilder.Append(", ");
                        }

                        logBuilder.Append(table[i].Item2);
                    }

                    logBuilder.Append(")");

                    logBuilder.AppendLine();
                }

                var title = $"Master Load : ({stopwatch.Elapsed.TotalMilliseconds:F1}ms)";

                void OutputCallback(string x)
                {
                    UnityConsole.Event(ConsoleEventName, ConsoleEventColor, x);
                }

                LogUtility.ChunkLog(logBuilder.ToString(), title, OutputCallback);

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

        /// <summary> キャッシュ削除 </summary>
        public void ClearMasterCache()
        {
            if (string.IsNullOrEmpty(InstallDirectory)){ return; }

            DeleteVersionFile();

            var isStreamingAssetsPath = InstallDirectory.StartsWith(UnityPathUtility.StreamingAssetsPath);

            if (!isStreamingAssetsPath)
            {
                DirectoryUtility.Clean(InstallDirectory);
            }
            
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

            await UniTask.RunOnThreadPool(async () =>
            {
                var tasks = new List<UniTask>();

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

                    }, false);

                    tasks.Add(task);
                }

                await UniTask.WhenAll(tasks);

            });

            return list.ToArray();
        }

        public void Clear()
        {
            if (masters == null){ return; }

            Reference.Clear();

            var items = masters.ToArray();

            foreach (var item in items)
            {
                item.Delete();
            }

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
