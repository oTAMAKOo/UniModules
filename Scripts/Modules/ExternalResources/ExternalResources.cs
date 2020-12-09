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
using Modules.Devkit;
using Modules.AssetBundles;
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


        // 外部アセットディレクトリ.
        private string resourceDir = null;

        // アセット保存先.
        private string installDir = null;

        private bool simulateMode = false;
        private bool localMode = false;

        private HashSet<AssetInfo> loadingAssets = new HashSet<AssetInfo>();

        // Coroutine中断用.
        private YieldCancel yieldCancel = null;

        // イベント通知.
        private Subject<AssetInfo> onTimeOut = null;
        private Subject<Exception> onError = null;

        private bool initialized = false;

        //----- property -----

        public static bool Initialized
        {
            get { return Instance != null && Instance.initialized; }
        }

        public string InstallDirectory { get { return installDir; } }

        //----- method -----

        private ExternalResources() { }

        public void Initialize(string resourceDir, string installDir, bool localMode = false)
        {
            if (initialized) { return; }

            this.resourceDir = resourceDir;
            this.installDir = installDir;
            this.localMode = localMode;

            // 中断用登録.
            yieldCancel = new YieldCancel();

            //----- AssetBundleManager初期化 -----
                        
            #if UNITY_EDITOR

            simulateMode = Prefs.isSimulate;

            #endif

            // AssetBundleManager初期化.
            assetBundleManager = AssetBundleManager.CreateInstance();
            assetBundleManager.Initialize(installDir, MaxDownloadCount, localMode, simulateMode);
            assetBundleManager.RegisterYieldCancel(yieldCancel);
            assetBundleManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
            assetBundleManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);

            #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

            // CriAssetManager初期化.

            criAssetManager = CriAssetManager.CreateInstance();
            criAssetManager.Initialize(installDir, resourceDir, MaxDownloadCount, localMode, simulateMode);
            criAssetManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
            criAssetManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);
            
            #endif

            // バージョン情報を読み込み.
            LoadVersion();

            initialized = true;
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

        /// <summary>
        /// キャッシュ削除.
        /// </summary>
        public void CleanCache()
        {
            UnloadAllAssetBundles(false);

            ClearVersion();

            DirectoryUtility.Clean(installDir);

            Caching.ClearCache();
        }

        /// <summary>
        /// マニフェストファイルを更新.
        /// </summary>
        public IObservable<Unit> UpdateManifest(IProgress<float> progress = null)
        {
            return Observable.FromCoroutine(() => UpdateManifestInternal(progress));
        }

        private IEnumerator UpdateManifestInternal(IProgress<float> progress = null)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            // アセット管理情報読み込み.
            
            var manifestFileName = AssetInfoManifest.ManifestFileName;

            #if UNITY_EDITOR

            if (simulateMode)
            {
                manifestFileName = PathUtility.Combine(resourceDir, manifestFileName);
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

            var message = string.Format("UpdateManifest: ({0}ms)", sw.Elapsed.TotalMilliseconds);
            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);

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
            if (instance.localMode) { return Observable.ReturnUnit(); }

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
        }

        private void CancelAllCoroutines()
        {
            if (yieldCancel != null)
            {
                yieldCancel.Dispose();

                // キャンセルしたので再生成.
                yieldCancel = new YieldCancel();

                assetBundleManager.RegisterYieldCancel(yieldCancel);
            }
        }

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

            var assetPath = PathUtility.Combine(resourceDir, resourcePath);

            // ローカルバージョンが古い場合はダウンロード.
            if (!CheckAssetBundleVersion(assetInfo) && !localMode)
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

                var builder = new StringBuilder();

                var assetBundleName = assetInfo.AssetBundle.AssetBundleName;

                builder.AppendFormat("Update: {0} ({1:F2}ms)", Path.GetFileName(assetPath), sw.Elapsed.TotalMilliseconds).AppendLine();
                builder.AppendLine();
                builder.AppendFormat("LoadPath = {0}", assetPath).AppendLine();
                builder.AppendFormat("FileName = {0}", assetInfo.FileName).AppendLine();
                builder.AppendFormat("AssetBundleName = {0}", assetBundleName).AppendLine();
                builder.AppendFormat("Hash = {0}", assetInfo.FileHash).AppendLine();

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
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
        }

        #endregion

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

        private string ConvertCriFilePath(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)){ return null; }

            var assetInfo = GetAssetInfo(resourcePath);

            return simulateMode ?
                PathUtility.Combine(new string[] { UnityPathUtility.GetProjectFolderPath(), resourceDir, resourcePath }) :
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

                if (!CheckAssetVersion(resourcePath, filePath) && !localMode)
                {
                    var assetInfo = GetAssetInfo(resourcePath);
                    var assetPath = PathUtility.Combine(resourceDir, resourcePath);

                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    var updateYield = UpdateAsset(resourcePath).ToYieldInstruction(false, yieldCancel.Token);

                    while (!updateYield.IsDone)
                    {
                        yield return null;
                    }

                    sw.Stop();

                    var builder = new StringBuilder();

                    builder.AppendFormat("Update: {0} ({1:F2}ms)", Path.GetFileName(filePath), sw.Elapsed.TotalMilliseconds).AppendLine();
                    builder.AppendLine();
                    builder.AppendFormat("LoadPath = {0}", assetPath).AppendLine();
                    builder.AppendFormat("FileName = {0}", assetInfo.FileName).AppendLine();
                    builder.AppendFormat("Hash = {0}", assetInfo.FileHash).AppendLine();

                    UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
                }

                filePath = PathUtility.GetPathWithoutExtension(filePath) + CriAssetDefinition.AcbExtension;

                observer.OnNext(File.Exists(filePath) ? new CueInfo(cue, filePath) : null);
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

                if (!localMode)
                {
                    if (!CheckAssetVersion(resourcePath, filePath))
                    {
                        var assetInfo = GetAssetInfo(resourcePath);
                        var assetPath = PathUtility.Combine(resourceDir, resourcePath);

                        var sw = System.Diagnostics.Stopwatch.StartNew();

                        var updateYield = UpdateAsset(resourcePath).ToYieldInstruction(false, yieldCancel.Token);

                        while (!updateYield.IsDone)
                        {
                            yield return null;
                        }

                        sw.Stop();

                        var builder = new StringBuilder();

                        builder.AppendFormat("Update: {0} ({1:F2}ms)", Path.GetFileName(filePath), sw.Elapsed.TotalMilliseconds).AppendLine();
                        builder.AppendLine();
                        builder.AppendFormat("LoadPath = {0}", assetPath).AppendLine();
                        builder.AppendFormat("FileName = {0}", assetInfo.FileName).AppendLine();
                        builder.AppendFormat("Hash = {0}", assetInfo.FileHash).AppendLine();

                        UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
                    }
                }

                filePath = PathUtility.GetPathWithoutExtension(filePath) + CriAssetDefinition.UsmExtension;

                observer.OnNext(File.Exists(filePath) ? new ManaInfo(filePath) : null);
            }

            observer.OnCompleted();
        }

        #endif

        #endregion
        
        private void OnTimeout(AssetInfo assetInfo)
        {
            CancelAllCoroutines();

            if (onTimeOut != null)
            {
                onTimeOut.OnNext(assetInfo);
            }
        }

        private void OnError(Exception exception)
        {
            CancelAllCoroutines();

            if (onError != null)
            {
                onError.OnNext(exception);
            }
        }

        /// <summary>
        /// タイムアウト時のイベント.
        /// </summary>
        public IObservable<AssetInfo> OnTimeOutAsObservable()
        {
            return onTimeOut ?? (onTimeOut = new Subject<AssetInfo>());
        }

        /// <summary>
        /// エラー時のイベント.
        /// </summary>
        public IObservable<Exception> OnErrorAsObservable()
        {
            return onError ?? (onError = new Subject<Exception>());
        }
    }
}
