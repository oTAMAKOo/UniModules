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

#if ENABLE_CRIWARE

using Modules.CriWare;
using Modules.SoundManagement;
using Modules.MovieManagement;

#endif

namespace Modules.ExternalResource
{
    public partial class ExternalResources : Singleton<ExternalResources>
    {
        //----- params -----

        public static readonly string ConsoleEventName = "ExternalResources";
        public static readonly Color ConsoleEventColor = new Color(127, 255, 212);

        //----- field -----

        // アセットバンドル管理.
        private AssetBundleManager assetBundleManager = null;
        // アセット管理情報.
        private AssetInfoManifest assetInfoManifest = null;

        // アセットバンドル名でグループ化したアセット情報.
        private ILookup<string, AssetInfo> assetInfosByAssetBundleName = null;

        // アセットロードパスをキーとしたアセット情報.
        private Dictionary<string, AssetInfo> assetInfosByResourcePath = null;

        #if ENABLE_CRIWARE

        // CriWare管理.
        private CriAssetManager criAssetManager = null;

        #endif
        
        // 外部アセットディレクトリ.
        private string resourceDir = null;

        private bool isSimulate = false;

        private HashSet<AssetInfo> loadingAssets = new HashSet<AssetInfo>();

        // Coroutine中断用.
        private YieldCancell yieldCancell = null;

        // イベント通知.
        private Subject<string> onTimeOut = null;
        private Subject<Exception> onError = null;

        private bool initialized = false;

        //----- property -----

        public static bool Initialized
        {
            get { return Instance != null && Instance.initialized; }
        }

        //----- method -----

        public void Initialize(string resourceDir)
        {
            if (initialized) { return; }

            this.resourceDir = resourceDir;

            // LZ4へ再圧縮有効.
            Caching.compressionEnabled = true;

            // 中断用登録.
            yieldCancell = new YieldCancell();

            //----- AssetBundleManager初期化 -----
                        
            #if UNITY_EDITOR

            isSimulate = Prefs.isSimulate;

            #endif

            // AssetBundleManager初期化.
            assetBundleManager = AssetBundleManager.CreateInstance();
            assetBundleManager.Initialize(simulateMode: isSimulate);
            assetBundleManager.RegisterYieldCancell(yieldCancell);
            assetBundleManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
            assetBundleManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);

            #if ENABLE_CRIWARE

            // CriAssetManager初期化.

            criAssetManager = CriAssetManager.CreateInstance();
            criAssetManager.Initialize(resourceDir, isSimulate);
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

            #if ENABLE_CRIWARE

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
            assetInfosByAssetBundleName = allAssetInfos.ToLookup(x => x.AssetBundleName);

            // アセット情報 (Key: リソースパス).
            assetInfosByResourcePath = allAssetInfos.ToDictionary(x => x.ResourcesPath);
        }

         /// <summary>
        /// アセット管理情報を取得.
        /// </summary>
        /// <param name="groupName"></param>
        /// <returns></returns>
        public IEnumerable<AssetInfo> GetAssetInfo(string groupName = null)
        {
            return assetInfoManifest.GetAssetInfos(groupName);
        }

        /// <summary>
        /// キャッシュ削除.
        /// </summary>
        public void CleanCache()
        {
            if (Exists)
            {
                UnloadAllAssetBundles(false);
            }

            ClearVersion();

            Caching.ClearCache();
        }

        /// <summary>
        /// マニフェストファイルを更新.
        /// </summary>
        public IObservable<Unit> UpdateManifest(UniRx.IProgress<float> progress = null)
        {
            return Observable.FromCoroutine(() => UpdateManifestInternal(progress));
        }

        private IEnumerator UpdateManifestInternal(UniRx.IProgress<float> progress = null)
        {
            var currentProgress = 0f;
            var scheduledNotifier = new ScheduledNotifier<float>();
            
            // 複数の非同期で100％の進捗度を示す.
            var notifierDisposable =  scheduledNotifier.Subscribe(
                x =>
                {
                    if (progress != null)
                    {
                        progress.Report(currentProgress + x * 0.5f);
                    }
                });

            var sw = System.Diagnostics.Stopwatch.StartNew();

            // AssetBundleマニフェストファイルを更新.
            currentProgress = 0f;

            if (!isSimulate)
            {
                yield return assetBundleManager.UpdateManifest(scheduledNotifier).ToYieldInstruction(false);
            }

            // アセット管理情報読み込み.
            currentProgress = 0.5f;

            var manifestAssetBundleName = AssetInfoManifest.AssetBundleName;
            var manifestFileName = AssetInfoManifest.ManifestFileName;

            #if UNITY_EDITOR

            if (isSimulate)
            {
                manifestFileName = PathUtility.Combine(resourceDir, manifestFileName);
            }

            #endif

            var loadYield = assetBundleManager
                .LoadAsset<AssetInfoManifest>(manifestAssetBundleName, manifestFileName, true, scheduledNotifier)
                .ToYieldInstruction(false);

            yield return loadYield;

            if (loadYield.HasError || loadYield.IsCanceled)
            {
                yield break;
            }

            SetAssetInfoManifest(loadYield.Result);

            sw.Stop();

            currentProgress = 1f;

            var message = string.Format("UpdateManifest: ({0}ms)", sw.Elapsed.TotalMilliseconds);
            UnityConsole.Event(ConsoleEventName, ConsoleEventColor, message);

            notifierDisposable.Dispose();

            #if ENABLE_CRIWARE

            criAssetManager.SetManifest(assetInfoManifest);

            #endif
        }

        /// <summary>
        /// アセットを更新.
        /// </summary>
        public static IObservable<Unit> UpdateAsset(string resourcesPath, UniRx.IProgress<float> progress = null)
        {
            if (string.IsNullOrEmpty(resourcesPath)) { return Observable.ReturnUnit(); }

            return Observable.FromCoroutine(() => instance.UpdateAssetInternal(resourcesPath, progress));
        }

        private IEnumerator UpdateAssetInternal(string resourcesPath, UniRx.IProgress<float> progress = null)
        {
            #if ENABLE_CRIWARE

            var extension = Path.GetExtension(resourcesPath);
            
            if (CriAssetDefinition.AssetAllExtensions.Any(x => x == extension))
            {
                var filePath = ConvertCriFilePath(resourcesPath);

                // ローカルバージョンが最新の場合は更新しない.
                if (CheckAssetVersion(resourcesPath, filePath))
                {
                    yield break;
                }

                var updateYield = instance.criAssetManager
                    .UpdateCriAsset(resourcesPath, progress)
                    .ToYieldInstruction(false, yieldCancell.Token);

                yield return updateYield;

                if (updateYield.IsCanceled || updateYield.HasError)
                {
                    yield break;
                }
            }
            else
            
            #endif

            {
                var assetInfo = FindAssetInfo(resourcesPath);

                if (assetInfo == null)
                {
                    Debug.LogErrorFormat("AssetManageInfo not found.\n{0}", resourcesPath);
                    yield break;
                }

                if (string.IsNullOrEmpty(assetInfo.AssetBundleName))
                {
                    Debug.LogErrorFormat("AssetBundleName is empty.\n{0}", resourcesPath);
                    yield break;
                }

                // ローカルバージョンが最新の場合は更新しない.
                if (CheckAssetBundleVersion(assetInfo.AssetBundleName))
                {
                    if(progress != null)
                    {
                        progress.Report(1f);
                    }

                    yield break;
                }

                var updateYield = instance.assetBundleManager
                    .UpdateAssetBundle(assetInfo.AssetBundleName, progress)
                    .ToYieldInstruction(false, yieldCancell.Token);

                yield return updateYield;

                if (updateYield.IsCanceled || updateYield.HasError)
                {
                    yield break;
                }
            }

            UpdateVersion(resourcesPath);
        }

        private void CancelAllCoroutines()
        {
            if (yieldCancell != null)
            {
                yieldCancell.Dispose();

                // キャンセルしたので再生成.
                yieldCancell = new YieldCancell();

                assetBundleManager.RegisterYieldCancell(yieldCancell);
            }
        }

        /// <summary> アセットが存在するか </summary>
        public static bool IsAssetExsist(string externalResourcesPath)
        {
            return Instance.IsAssetExsistInternal(externalResourcesPath);
        }

        private bool IsAssetExsistInternal(string resourcesPath)
        {
            if (assetInfoManifest == null){ return false; }
           
            var assetInfo = FindAssetInfo(resourcesPath);

            return assetInfo != null;
        }

        private AssetInfo FindAssetInfo(string resourcesPath)
        {
            return assetInfosByResourcePath.GetValueOrDefault(resourcesPath);
        }

        #region AssetBundle

        /// <summary> Assetbundleを読み込み (非同期) </summary>
        public static IObservable<T> LoadAsset<T>(string externalResourcesPath, bool autoUnload = true) where T : UnityEngine.Object
        {
            return Observable.FromCoroutine<T>(observer => Instance.LoadAssetInternal(observer, externalResourcesPath, autoUnload));
        }

        private IEnumerator LoadAssetInternal<T>(IObserver<T> observer, string resourcesPath, bool autoUnload) where T : UnityEngine.Object
        {
            T result = null;

            if (assetInfoManifest == null)
            {
                var exception = new Exception("AssetInfoManifest is null.");

                if (onError != null)
                {
                    onError.OnNext(exception);
                }

                observer.OnError(exception);

                yield break;
            }

            var assetInfo = FindAssetInfo(resourcesPath);

            if (assetInfo == null)
            {
                var exception = new Exception(string.Format("AssetInfo not found.\n{0}", resourcesPath));

                if (onError != null)
                {
                    onError.OnNext(exception);
                }

                observer.OnError(exception);

                yield break;
            }

            // ローカルバージョンが古い場合はエラー扱い.
            // ※ 読み込むことは出来るが明示的にUpdateAssetを実行させる為エラー扱いにする.
            if (!CheckAssetBundleVersion(assetInfo.AssetBundleName))
            {
                var exception = new Exception(string.Format("The version of this asset is outdated.\n{0}", resourcesPath));

                if (onError != null)
                {
                    onError.OnNext(exception);
                }

                observer.OnError(exception);

                yield break;
            }

            var assetPath = PathUtility.Combine(resourceDir, resourcesPath);

            var isLoading = loadingAssets.Contains(assetInfo);

            if(!isLoading)
            {
                loadingAssets.Add(assetInfo);
            }

            // 読み込み実行 (読み込み中の場合は読み込み待ちのObservableが返る).
            var sw = System.Diagnostics.Stopwatch.StartNew();

            var loadYield = assetBundleManager.LoadAsset<T>(assetInfo.AssetBundleName, assetPath, autoUnload).ToYieldInstruction();

            yield return loadYield;

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

                builder.AppendFormat("Load: {0} ({1}ms)", Path.GetFileName(assetPath), sw.Elapsed.TotalMilliseconds).AppendLine();
                builder.AppendLine();
                builder.AppendFormat("LoadPath = {0}", assetPath).AppendLine();
                builder.AppendFormat("AssetBundleName = {0}", assetInfo.AssetBundleName).AppendLine();
                builder.AppendFormat("Hash = {0}", assetInfo.FileHash).AppendLine();

                if (!string.IsNullOrEmpty(assetInfo.GroupName))
                {
                    builder.AppendFormat("Group = {0}", assetInfo.GroupName).AppendLine();
                }

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
            }

            observer.OnNext(result);
            observer.OnCompleted();
        }

        /// <summary> Assetbundleを解放 </summary>
        public static void UnloadAssetBundle(string externalResourcesPath)
        {
            Instance.UnloadAssetInternal(externalResourcesPath);
        }

        /// <summary> 全てのAssetbundleを解放 </summary>
        public static void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)
        {
            Instance.assetBundleManager.UnloadAllAsset(unloadAllLoadedObjects);
        }

        /// <summary> 読み込み済みAssetbundle一覧取得 </summary>
        public static UniRx.Tuple<string, int>[] GetLoadedAssets()
        {
            return Instance.assetBundleManager.GetLoadedAssetBundleNames();
        }

        private void UnloadAssetInternal(string resourcesPath)
        {
            if (assetInfoManifest == null)
            {
                Debug.LogError("AssetInfoManifest is null.");
            }

            var assetInfo = FindAssetInfo(resourcesPath);

            if (assetInfo == null)
            {
                Debug.LogErrorFormat("AssetInfo not found.\n{0}", resourcesPath);
            }

            assetBundleManager.UnloadAsset(assetInfo.AssetBundleName);
        }

        #endregion

        #if ENABLE_CRIWARE

        private string ConvertCriFilePath(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath)){ return null; }

            return isSimulate ?
                PathUtility.Combine(new string[] { UnityPathUtility.GetProjectFolderPath(), resourceDir, resourcesPath }) :
                PathUtility.Combine(new string[] { CriAssetManager.GetInstallDirectory(), resourcesPath });
        }

        #region Sound
        
        public static CueInfo GetCueInfo(string resourcesPath, string cue)
        {
            return Instance.GetCueInfoInternal(resourcesPath, cue);
        }

        private CueInfo GetCueInfoInternal(string resourcesPath, string cue)
        {
            if (string.IsNullOrEmpty(resourcesPath)){ return null; }

            var filePath = ConvertCriFilePath(resourcesPath);

            filePath = PathUtility.GetPathWithoutExtension(filePath) + CriAssetDefinition.AcbExtension;
            
            return File.Exists(filePath) ? new CueInfo(cue, filePath) : null;
        }

        #endregion

        #region Movie

        public static ManaInfo GetMovieInfo(string resourcesPath)
        {
            return Instance.GetMovieInfoInternal(resourcesPath);
        }

        private ManaInfo GetMovieInfoInternal(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath)) { return null; }

            var filePath = ConvertCriFilePath(resourcesPath);

            filePath = PathUtility.GetPathWithoutExtension(filePath) + CriAssetDefinition.UsmExtension;

            return File.Exists(filePath) ? new ManaInfo(filePath) : null;
        }

        #endregion

        #endif

        private void OnTimeout(string str)
        {
            CancelAllCoroutines();

            if (onTimeOut != null)
            {
                onTimeOut.OnNext(str);
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
        public IObservable<string> OnTimeOutAsObservable()
        {
            return onTimeOut ?? (onTimeOut = new Subject<string>());
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