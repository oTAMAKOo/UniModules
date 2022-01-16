
using UnityEngine;
using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Linq;
using UniRx;
using Modules.Devkit.Console;
using Modules.AssetBundles;

namespace Modules.ExternalResource
{
    public sealed partial class ExternalResources
    {
        //----- params -----

        //----- field -----

        // アセットバンドル管理.
        private AssetBundleManager assetBundleManager = null;

        // アセットバンドル名でグループ化したアセット情報.
        private ILookup<string, AssetInfo> assetInfosByAssetBundleName = null;

        //----- property -----

        //----- method -----

        private void InitializeAssetBundle()
        {
            assetBundleManager = AssetBundleManager.CreateInstance();
            assetBundleManager.Initialize(MaxDownloadCount, simulateMode);
            assetBundleManager.RegisterYieldCancel(yieldCancel);
            assetBundleManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
            assetBundleManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);
        }

        /// <summary> AssetBundleを読み込み (非同期) </summary>
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

                OnError(exception);

                observer.OnError(exception);

                yield break;
            }

            var assetInfo = GetAssetInfo(resourcePath);

            if (assetInfo == null)
            {
                var exception = new Exception(string.Format("AssetInfo not found.\n{0}", resourcePath));

                OnError(exception);

                observer.OnError(exception);

                yield break;
            }

            // 外部処理.

            if (instance.loadAssetHandler != null)
            {
                var loadRequestYield = instance.loadAssetHandler
                    .OnLoadRequest(assetInfo)
                    .ToYieldInstruction(false, yieldCancel.Token);

                while (!loadRequestYield.IsDone)
                {
                    yield return null;
                }

                if (loadRequestYield.HasError)
                {
                    OnError(loadRequestYield.Error);

                    observer.OnError(loadRequestYield.Error);

                    yield break;
                }
            }

            // 読み込み.

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
                        OnError(downloadYield.Error);

                        observer.OnError(downloadYield.Error);

                        yield break;
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

                        if (!string.IsNullOrEmpty(assetInfo.Hash))
                        {
                            builder.AppendFormat("Hash = {0}", assetInfo.Hash).AppendLine();
                        }

                        UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
                    }
                }
            }

            var isLoading = loadingAssets.Contains(assetInfo);

            if (!isLoading)
            {
                loadingAssets.Add(assetInfo);
            }

            // 時間計測開始.

            sw = System.Diagnostics.Stopwatch.StartNew();

            // 読み込み実行 (読み込み中の場合は読み込み待ちのObservableが返る).

            var loadYield = assetBundleManager.LoadAsset<T>(assetInfo, assetPath, autoUnload).ToYieldInstruction();

            while (!loadYield.IsDone)
            {
                yield return null;
            }

            result = loadYield.Result;

            // 読み込み中リストから外す.

            if (loadingAssets.Contains(assetInfo))
            {
                loadingAssets.Remove(assetInfo);
            }

            // 外部処理.

            if (instance.loadAssetHandler != null)
            {
                var loadFinishYield = instance.loadAssetHandler
                    .OnLoadFinish(assetInfo)
                    .ToYieldInstruction(false, yieldCancel.Token);

                while (!loadFinishYield.IsDone)
                {
                    yield return null;
                }

                if (loadFinishYield.HasError)
                {
                    OnError(loadFinishYield.Error);

                    observer.OnError(loadFinishYield.Error);

                    yield break;
                }
            }

            // 時間計測終了.

            sw.Stop();

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
                    builder.AppendFormat("Hash = {0}", assetInfo.Hash).AppendLine();

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

        /// <summary> AssetBundleを解放 </summary>
        public static void UnloadAssetBundle(string resourcePath)
        {
            Instance.UnloadAssetInternal(resourcePath);
        }

        /// <summary> 全てのAssetBundleを解放 </summary>
        public static void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)
        {
            Instance.UnloadAllAssetsInternal(unloadAllLoadedObjects);
        }

        private void UnloadAllAssetsInternal(bool unloadAllLoadedObjects)
        {
            var assetBundleManager = Instance.assetBundleManager;

            if (onUnloadAsset != null)
            {
                var loadedAssets = GetLoadedAssets();

                foreach (var loadedAsset in loadedAssets)
                {
                    onUnloadAsset.OnNext(loadedAsset.Item1);
                }
            }

            if (assetBundleManager != null)
            {
                assetBundleManager.UnloadAllAsset(unloadAllLoadedObjects);
            }
        }

        /// <summary> 読み込み済みAssetBundle一覧取得 </summary>
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
    }
}
