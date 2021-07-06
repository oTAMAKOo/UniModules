﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿
using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UniRx;
using Extensions;
using Modules.AssetBundles;
using Modules.Devkit.Console;
using Modules.UniRxExtension;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC
using Modules.CriWare;
#endif

#if ENABLE_CRIWARE_ADX
using Modules.SoundManagement;
#endif

#if ENABLE_CRIWARE_SOFDEC
using Modules.MovieManagement;
#endif

namespace Modules.ExternalResource
{
    public sealed partial class ExternalResources : Singleton<ExternalResources>
    {
        //----- params -----

        public static readonly string ConsoleEventName = "ExternalResources";
        public static readonly Color ConsoleEventColor = new Color(0.8f, 1f, 0.1f);

        public const string ShareCategoryName = "Share";

        public const string ShareCategoryPrefix = ShareCategoryName + ":";

        // 最大同時ダウンロード数.
        private readonly uint MaxDownloadCount = 4;

        //----- field -----

        // アセットバンドル管理.
        private AssetBundleManager assetBundleManager = null;
        // アセット管理情報.
        private AssetInfoManifest assetInfoManifest = null;

        // アセットバンドル名でグループ化したアセット情報.
        private ILookup<string, AssetInfo> assetInfosByAssetBundleName = null;

        // アセットロードパスをキーとしたアセット情報.
        private Dictionary<string, AssetInfo> assetInfosByResourcePath = null;

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

        // CriWare管理.
        private CriAssetManager criAssetManager = null;

        #endif

        /// <summary> シュミレーションモード (Editorのみ有効). </summary>
        private bool simulateMode = false;

        /// <summary> 外部アセットディレクトリ. </summary>
        public string resourceDirectory = null;
        /// <summary> 共有外部アセットディレクトリ. </summary>
        public string shareDirectory = null;

        /// <summary> 読み込み中アセット群. </summary>
        private HashSet<AssetInfo> loadingAssets = new HashSet<AssetInfo>();

        // Coroutine中断用.
        private YieldCancel yieldCancel = null;

        // イベント通知.
        private Subject<AssetInfo> onTimeOut = null;
        private Subject<Exception> onError = null;

        private Subject<string> onUpdateAsset = null;
        private Subject<string> onLoadAsset = null;
        private Subject<string> onUnloadAsset = null;

        private bool initialized = false;

        //----- property -----

        public static bool Initialized
        {
            get { return Instance != null && Instance.initialized; }
        }

        /// <summary> ローカルモード. </summary>
        public bool LocalMode { get; private set; }

        /// <summary> ダウンロード先. </summary>
        public string InstallDirectory { get; private set; }

        /// <summary> ログ出力が有効. </summary>
        public bool LogEnable { get; set; }

        //----- method -----

        private ExternalResources()
        {
            LogEnable = true;
        }

        public void Initialize(string resourceDirectory, string shareDirectory)
        {
            if (initialized) { return; }

            this.resourceDirectory = resourceDirectory;
            this.shareDirectory = shareDirectory;

            // 中断用登録.
            yieldCancel = new YieldCancel();

            //----- AssetBundleManager初期化 -----
                        
            #if UNITY_EDITOR

            simulateMode = Prefs.isSimulate;

            #endif

            // AssetBundleManager初期化.
            assetBundleManager = AssetBundleManager.CreateInstance();
            assetBundleManager.Initialize(MaxDownloadCount, simulateMode);
            assetBundleManager.RegisterYieldCancel(yieldCancel);
            assetBundleManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
            assetBundleManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            // CriAssetManager初期化.

            criAssetManager = CriAssetManager.CreateInstance();
            criAssetManager.Initialize(resourceDirectory, MaxDownloadCount, simulateMode);
            criAssetManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
            criAssetManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);
            
            #endif

            // 保存先設定.
            SetInstallDirectory(Application.persistentDataPath);

            // バージョン情報を読み込み.
            LoadVersion();

            initialized = true;
        }

        /// <summary> ローカルモード設定. </summary>
        public void SetLocalMode(bool localMode)
        {
            LocalMode = localMode;

            assetBundleManager.SetLocalMode(localMode);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            criAssetManager.SetLocalMode(localMode);

            #endif
        }

        /// <summary> 保存先ディレクトリ設定. </summary>
        public void SetInstallDirectory(string directory)
        {
            InstallDirectory = PathUtility.Combine(directory, "ExternalResources");

            #if UNITY_IOS

            if (InstallDirectory.StartsWith(Application.persistentDataPath))
            {
                UnityEngine.iOS.Device.SetNoBackupFlag(InstallDirectory);
            }

            #endif

            assetBundleManager.SetInstallDirectory(InstallDirectory);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            criAssetManager.SetInstallDirectory(InstallDirectory);

            #endif
        }

        /// <summary>
        /// URLを設定.
        /// </summary>
        /// <param name="remoteUrl"></param>
        public void SetUrl(string remoteUrl)
        {
            assetBundleManager.SetUrl(remoteUrl);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            criAssetManager.SetUrl(remoteUrl);

            #endif
        }

        // アセット管理マニュフェスト情報を更新.
        private void SetAssetInfoManifest(AssetInfoManifest manifest)
        {
            assetInfoManifest = manifest;

            if(manifest == null)
            {
                Debug.LogError("AssetInfoManifest not found.");
                return;
            }

            var allAssetInfos = manifest.GetAssetInfos().ToArray();

            // アセット情報 (Key: アセットバンドル名).
            assetInfosByAssetBundleName = allAssetInfos
                .Where(x => x.IsAssetBundle)
                .ToLookup(x => x.AssetBundle.AssetBundleName);

            // アセット情報 (Key: リソースパス).
            assetInfosByResourcePath = allAssetInfos.ToDictionary(x => x.ResourcePath);

            // アセットバンドル依存関係.
            var dependencies = allAssetInfos
                .Where(x => x.IsAssetBundle)
                .Select(x => x.AssetBundle)
                .Where(x => x.Dependencies != null && x.Dependencies.Any())
                .GroupBy(x => x.AssetBundleName)
                .Select(x => x.FirstOrDefault())
                .ToDictionary(x => x.AssetBundleName, x => x.Dependencies);

            assetBundleManager.SetDependencies(dependencies);
        }

         /// <summary>
        /// アセット管理情報を取得.
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public IEnumerable<AssetInfo> GetGroupAssetInfos(string groupName = null)
        {
            return assetInfoManifest.GetAssetInfos(groupName);
        }

        /// <summary> アセット情報取得 </summary>
        public AssetInfo GetAssetInfo(string resourcePath)
        {
            return assetInfosByResourcePath.GetValueOrDefault(resourcePath);
        }

        /// <summary> キャッシュ削除. </summary>
        public void CleanCache()
        {
            UnloadAllAssetBundles(false);

            ClearVersion();

            DirectoryUtility.Clean(InstallDirectory);

            Caching.ClearCache();

            GC.Collect();
        }

        /// <summary>
        /// マニフェストファイルを更新.
        /// </summary>
        public IObservable<Unit> UpdateManifest()
        {
            return Observable.FromCoroutine(() => UpdateManifestInternal());
        }

        private IEnumerator UpdateManifestInternal()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // アセット管理情報読み込み.
            
            var manifestFileName = AssetInfoManifest.ManifestFileName;

            #if UNITY_EDITOR

            if (simulateMode)
            {
                manifestFileName = PathUtility.Combine(resourceDirectory, manifestFileName);
            }

            #endif

            var manifestAssetInfo = AssetInfoManifest.GetManifestAssetInfo();

            // AssetInfoManifestは常に最新に保たなくてはいけない為必ずダウンロードする.
            var loadYield = assetBundleManager.UpdateAssetInfoManifest()
                .SelectMany(_ => assetBundleManager.LoadAsset<AssetInfoManifest>(manifestAssetInfo, manifestFileName))
                .ToYieldInstruction(false);

            yield return loadYield;

            if (loadYield.HasError || loadYield.IsCanceled)
            {
                yield break;
            }

            SetAssetInfoManifest(loadYield.Result);

            sw.Stop();

            if (LogEnable && UnityConsole.Enable)
            {
                var message = string.Format("UpdateManifest: ({0}ms)", sw.Elapsed.TotalMilliseconds);

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);
            }

            // アセット管理情報を登録.

            assetBundleManager.SetManifest(assetInfoManifest);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            criAssetManager.SetManifest(assetInfoManifest);

            #endif
        }

        /// <summary>
        /// アセットを更新.
        /// </summary>
        public static IObservable<Unit> UpdateAsset(string resourcePath, IProgress<float> progress = null)
        {
            if (instance.LocalMode) { return Observable.ReturnUnit(); }

            if (string.IsNullOrEmpty(resourcePath)) { return Observable.ReturnUnit(); }

            return Observable.FromCoroutine(() => instance.UpdateAssetInternal(resourcePath, progress));
        }

        private IEnumerator UpdateAssetInternal(string resourcePath, IProgress<float> progress = null)
        {
            #if ENABLE_CRIWARE_FILESYSTEM

            var extension = Path.GetExtension(resourcePath);
            
            if (CriAssetDefinition.AssetAllExtensions.Any(x => x == extension))
            {
                var filePath = ConvertCriFilePath(resourcePath);

                // ローカルバージョンが最新の場合は更新しない.
                if (CheckAssetVersion(resourcePath, filePath))
                {
                    yield break;
                }

                var updateYield = instance.criAssetManager
                    .UpdateCriAsset(resourcePath, progress)
                    .ToYieldInstruction(false, yieldCancel.Token);

                while (!updateYield.IsDone)
                {
                    yield return null;
                }

                if (updateYield.IsCanceled || updateYield.HasError)
                {
                    yield break;
                }
            }
            else
            
            #endif

            {
                var assetInfo = GetAssetInfo(resourcePath);

                if (assetInfo == null)
                {
                    Debug.LogErrorFormat("AssetManageInfo not found.\n{0}", resourcePath);
                    yield break;
                }

                // ローカルバージョンが最新の場合は更新しない.
                if (CheckAssetBundleVersion(assetInfo))
                {
                    if(progress != null)
                    {
                        progress.Report(1f);
                    }

                    yield break;
                }

                var updateYield = instance.assetBundleManager
                    .UpdateAssetBundle(assetInfo, progress)
                    .ToYieldInstruction(false, yieldCancel.Token);

                yield return updateYield;

                if (updateYield.IsCanceled || updateYield.HasError)
                {
                    yield break;
                }
            }

            UpdateVersion(resourcePath);

            if (onUpdateAsset != null)
            {
                onUpdateAsset.OnNext(resourcePath);
            }
        }

        private void CancelAllCoroutine()
        {
            if (yieldCancel != null)
            {
                yieldCancel.Dispose();

                // キャンセルしたので再生成.
                yieldCancel = new YieldCancel();

                assetBundleManager.RegisterYieldCancel(yieldCancel);
            }
        }

        public static string GetAssetPathFromAssetInfo(string externalResourcesPath, string shareResourcesPath, AssetInfo assetInfo)
        {
            var assetPath = string.Empty;

            if (HasSharePrefix(assetInfo.ResourcePath))
            {
                var path = ConvertToShareResourcePath(assetInfo.ResourcePath);

                assetPath = PathUtility.Combine(shareResourcesPath, path);
            }
            else
            {
                assetPath = PathUtility.Combine(externalResourcesPath, assetInfo.ResourcePath);
            }

            return assetPath;
        }

        #region Share

        private static bool HasSharePrefix(string resourcePath)
        {
            return resourcePath.StartsWith(ShareCategoryPrefix);
        }

        private static string ConvertToShareResourcePath(string resourcePath)
        {
            if (HasSharePrefix(resourcePath))
            {
                resourcePath = resourcePath.Substring(ShareCategoryPrefix.Length);
            }

            return resourcePath;
        }

        #endregion

        #region AssetBundle

        /// <summary> Assetbundleを読み込み (非同期) </summary>
        public static IObservable<T> LoadAsset<T>(string resourcePath, bool autoUnload = true) where T : UnityEngine.Object
        {
            return Observable.FromMicroCoroutine<T>(observer => Instance.LoadAssetInternal(observer, resourcePath, autoUnload));
        }

        private IEnumerator LoadAssetInternal<T>(IObserver<T> observer, string resourcePath, bool autoUnload) where T : UnityEngine.Object
        {
            System.Diagnostics.Stopwatch sw = null;

            T result = null;

            if (assetInfoManifest == null)
            {
                var exception = new Exception("AssetInfoManifest is null.");

                Debug.LogException(exception);

                if (onError != null)
                {
                    onError.OnNext(exception);
                }

                observer.OnError(exception);

                yield break;
            }

            var assetInfo = GetAssetInfo(resourcePath);

            if (assetInfo == null)
            {
                var exception = new Exception(string.Format("AssetInfo not found.\n{0}", resourcePath));

                Debug.LogException(exception);

                if (onError != null)
                {
                    onError.OnNext(exception);
                }

                observer.OnError(exception);

                yield break;
            }

            var assetPath = GetAssetPathFromAssetInfo(resourceDirectory, shareDirectory, assetInfo);

            if (!LocalMode && !simulateMode)
            {
                // ローカルバージョンが古い場合はダウンロード.
                if (!CheckAssetBundleVersion(assetInfo))
                {
                    var downloadYield = UpdateAsset(resourcePath).ToYieldInstruction(false, yieldCancel.Token);

                    // 読み込み実行 (読み込み中の場合は読み込み待ちのObservableが返る).
                    sw = System.Diagnostics.Stopwatch.StartNew();

                    while (!downloadYield.IsDone)
                    {
                        yield return null;
                    }

                    if (downloadYield.HasError)
                    {
                        Debug.LogException(downloadYield.Error);

                        if (onError != null)
                        {
                            onError.OnNext(downloadYield.Error);
                        }

                        observer.OnError(downloadYield.Error);
                    }

                    sw.Stop();

                    if (LogEnable && UnityConsole.Enable)
                    {
                        var builder = new StringBuilder();

                        var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

                        builder.AppendFormat("Update: {0} ({1:F2}ms)", Path.GetFileName(assetPath), sw.Elapsed.TotalMilliseconds).AppendLine();
                        builder.AppendLine();
                        builder.AppendFormat("LoadPath = {0}", assetPath).AppendLine();
                        builder.AppendFormat("FileName = {0}", assetInfo.FileName).AppendLine();
                        builder.AppendFormat("AssetBundleName = {0}", assetBundleName).AppendLine();

                        if (!string.IsNullOrEmpty(assetInfo.FileHash))
                        {
                            builder.AppendFormat("Hash = {0}", assetInfo.FileHash).AppendLine();
                        }

                        UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
                    }
                }
            }

            var isLoading = loadingAssets.Contains(assetInfo);

            if(!isLoading)
            {
                loadingAssets.Add(assetInfo);
            }

            // 読み込み実行 (読み込み中の場合は読み込み待ちのObservableが返る).
            sw = System.Diagnostics.Stopwatch.StartNew();

            var loadYield = assetBundleManager.LoadAsset<T>(assetInfo, assetPath, autoUnload).ToYieldInstruction();

            while (!loadYield.IsDone)
            {
                yield return null;
            }

            result = loadYield.Result;

            sw.Stop();

            if (loadingAssets.Contains(assetInfo))
            {
                loadingAssets.Remove(assetInfo);
            }

            // 読み込み中だった場合はログを表示しない.
            if (result != null && !isLoading)
            {
                if (LogEnable && UnityConsole.Enable)
                {
                    var builder = new StringBuilder();

                    var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

                    builder.AppendFormat("Load: {0} ({1:F2}ms)", Path.GetFileName(assetPath), sw.Elapsed.TotalMilliseconds).AppendLine();
                    builder.AppendLine();
                    builder.AppendFormat("LoadPath = {0}", assetPath).AppendLine();
                    builder.AppendFormat("AssetBundleName = {0}", assetBundleName).AppendLine();
                    builder.AppendFormat("Hash = {0}", assetInfo.FileHash).AppendLine();

                    if (!string.IsNullOrEmpty(assetInfo.Category))
                    {
                        builder.AppendFormat("Category = {0}", assetInfo.Category).AppendLine();
                    }

                    UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
                }

                if (onLoadAsset != null)
                {
                    onLoadAsset.OnNext(resourcePath);
                }
            }

            observer.OnNext(result);
            observer.OnCompleted();
        }

        /// <summary> Assetbundleを解放 </summary>
        public static void UnloadAssetBundle(string resourcePath)
        {
            Instance.UnloadAssetInternal(resourcePath);
        }

        /// <summary> 全てのAssetbundleを解放 </summary>
        public static void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)
        {
            Instance.UnloadAllAssetsInternal(unloadAllLoadedObjects);
        }

        private void UnloadAllAssetsInternal(bool unloadAllLoadedObjects)
        {
            if (onUnloadAsset != null)
            {
                var loadedAssets = GetLoadedAssets();

                foreach (var loadedAsset in loadedAssets)
                {
                    onUnloadAsset.OnNext(loadedAsset.Item1);
                }
            }

            Instance.assetBundleManager.UnloadAllAsset(unloadAllLoadedObjects);
        }

        /// <summary> 読み込み済みAssetbundle一覧取得 </summary>
        public static Tuple<string, int>[] GetLoadedAssets()
        {
            return Instance.assetBundleManager.GetLoadedAssetBundleNames();
        }

        private void UnloadAssetInternal(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) { return; }

            if (assetInfoManifest == null)
            {
                Debug.LogError("AssetInfoManifest is null.");
            }

            var assetInfo = GetAssetInfo(resourcePath);

            if (assetInfo == null)
            {
                Debug.LogErrorFormat("AssetInfo not found.\n{0}", resourcePath);
            }

            if (!assetInfo.IsAssetBundle)
            {
                Debug.LogErrorFormat("This file is not an assetBundle.\n{0}", resourcePath);
            }

            assetBundleManager.UnloadAsset(assetInfo.AssetBundle.AssetBundleName);

            if (onUnloadAsset != null)
            {
                onUnloadAsset.OnNext(resourcePath);
            }
        }

        #endregion

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

        private string ConvertCriFilePath(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)){ return null; }

            var assetInfo = GetAssetInfo(resourcePath);

            return simulateMode ?
                PathUtility.Combine(new string[] { UnityPathUtility.GetProjectFolderPath(), resourceDirectory, resourcePath }) :
                criAssetManager.BuildFilePath(assetInfo);
        }

        #endif

        #region Sound

        #if ENABLE_CRIWARE_ADX
        
        public static IObservable<CueInfo> GetCueInfo(string resourcePath, string cue)
        {
            return Observable.FromMicroCoroutine<CueInfo>(observer => Instance.GetCueInfoInternal(observer, resourcePath, cue));
        }

        private IEnumerator GetCueInfoInternal(IObserver<CueInfo> observer, string resourcePath, string cue)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                observer.OnError(new ArgumentException("resourcePath"));
            }
            else
            {
                var filePath = ConvertCriFilePath(resourcePath);

                if (!LocalMode && !simulateMode)
                {
                    if (!CheckAssetVersion(resourcePath, filePath))
                    {
                        var assetInfo = GetAssetInfo(resourcePath);
                        var assetPath = PathUtility.Combine(resourceDirectory, resourcePath);

                        var sw = System.Diagnostics.Stopwatch.StartNew();

                        var updateYield = UpdateAsset(resourcePath).ToYieldInstruction(false, yieldCancel.Token);

                        while (!updateYield.IsDone)
                        {
                            yield return null;
                        }

                        sw.Stop();

                        if (LogEnable && UnityConsole.Enable)
                        {
                            var builder = new StringBuilder();

                            builder.AppendFormat("Update: {0} ({1:F2}ms)", Path.GetFileName(filePath), sw.Elapsed.TotalMilliseconds).AppendLine();
                            builder.AppendLine();
                            builder.AppendFormat("LoadPath = {0}", assetPath).AppendLine();
                            builder.AppendFormat("FileName = {0}", assetInfo.FileName).AppendLine();

                            if (!string.IsNullOrEmpty(assetInfo.FileHash))
                            {
                                builder.AppendFormat("Hash = {0}", assetInfo.FileHash).AppendLine();
                            }

                            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
                        }
                    }
                }

                filePath = PathUtility.GetPathWithoutExtension(filePath) + CriAssetDefinition.AcbExtension;

                observer.OnNext(File.Exists(filePath) ? new CueInfo(cue, filePath) : null);

                if (onLoadAsset != null)
                {
                    onLoadAsset.OnNext(resourcePath);
                }
            }

            observer.OnCompleted();
        }

        #endif

        #endregion

        #region Movie

        #if ENABLE_CRIWARE_SOFDEC

        public static IObservable<ManaInfo> GetMovieInfo(string resourcePath)
        {
            return Observable.FromMicroCoroutine<ManaInfo>(observer => Instance.GetMovieInfoInternal(observer, resourcePath));
        }

        private IEnumerator GetMovieInfoInternal(IObserver<ManaInfo> observer, string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                observer.OnError(new ArgumentException("resourcePath"));
            }
            else
            {
                var filePath = ConvertCriFilePath(resourcePath);

                if (!LocalMode && !simulateMode)
                {
                    if (!CheckAssetVersion(resourcePath, filePath))
                    {
                        var assetInfo = GetAssetInfo(resourcePath);
                        var assetPath = PathUtility.Combine(resourceDirectory, resourcePath);

                        var sw = System.Diagnostics.Stopwatch.StartNew();

                        var updateYield = UpdateAsset(resourcePath).ToYieldInstruction(false, yieldCancel.Token);

                        while (!updateYield.IsDone)
                        {
                            yield return null;
                        }

                        sw.Stop();

                        if (LogEnable && UnityConsole.Enable)
                        {
                            var builder = new StringBuilder();

                            builder.AppendFormat("Update: {0} ({1:F2}ms)", Path.GetFileName(filePath), sw.Elapsed.TotalMilliseconds).AppendLine();
                            builder.AppendLine();
                            builder.AppendFormat("LoadPath = {0}", assetPath).AppendLine();
                            builder.AppendFormat("FileName = {0}", assetInfo.FileName).AppendLine();

                            if (!string.IsNullOrEmpty(assetInfo.FileHash))
                            {
                                builder.AppendFormat("Hash = {0}", assetInfo.FileHash).AppendLine();
                            }

                            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
                        }
                    }
                }

                filePath = PathUtility.GetPathWithoutExtension(filePath) + CriAssetDefinition.UsmExtension;

                if (File.Exists(filePath))
                {
                    var manaInfo = new ManaInfo(filePath);

                    observer.OnNext(manaInfo);

                    if (onLoadAsset != null)
                    {
                        onLoadAsset.OnNext(resourcePath);
                    }
                }
                else
                {
                    Debug.LogErrorFormat("File not found.\n{0}", filePath);

                    observer.OnError(new FileNotFoundException(filePath));
                }
            }

            observer.OnCompleted();
        }

        #endif

        #endregion
        
        private void OnTimeout(AssetInfo assetInfo)
        {
            CancelAllCoroutine();

            if (onTimeOut != null)
            {
                onTimeOut.OnNext(assetInfo);
            }
        }

        private void OnError(Exception exception)
        {
            CancelAllCoroutine();

            if (onError != null)
            {
                onError.OnNext(exception);
            }
        }

        /// <summary> タイムアウト時イベント. </summary>
        public IObservable<AssetInfo> OnTimeOutAsObservable()
        {
            return onTimeOut ?? (onTimeOut = new Subject<AssetInfo>());
        }

        /// <summary> エラー時イベント. </summary>
        public IObservable<Exception> OnErrorAsObservable()
        {
            return onError ?? (onError = new Subject<Exception>());
        }

        /// <summary> アセット更新時イベント. </summary>
        public IObservable<string> OnUpdateAssetAsObservable()
        {
            return onUpdateAsset ?? (onUpdateAsset = new Subject<string>());
        }

        /// <summary> アセット読み込み時イベント. </summary>
        public IObservable<string> OnLoadAssetAsObservable()
        {
            return onLoadAsset ?? (onLoadAsset = new Subject<string>());
        }

        /// <summary> アセット解放時イベント. </summary>
        public IObservable<string> OnUnloadAssetAsObservable()
        {
            return onUnloadAsset ?? (onUnloadAsset = new Subject<string>());
        }
    }
}
