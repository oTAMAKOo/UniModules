
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.CriWare;
using Modules.Devkit.Console;

#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE
using Modules.Sound;
#endif

#if ENABLE_CRIWARE_SOFDEC
using Modules.Movie;
#endif

namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset
    {
        //----- params -----

        /// <summary> CRIデフォルトインストーラ数.  </summary>
        private const uint CriDefaultInstallerCount = 8;

        //----- field -----

        // CriWare管理.
        private CriAssetManager criAssetManager = null;

        //----- property -----

        //----- method -----

        private void InitializeCri()
        {
            // CriAssetManager初期化.

            criAssetManager = CriAssetManager.CreateInstance();
            criAssetManager.Initialize(SimulateMode);
            criAssetManager.SetNumInstallers(CriDefaultInstallerCount);
            criAssetManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
            criAssetManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);
        }

        public void SetCriInstallerCount(uint installerCount)
        {
            criAssetManager.SetNumInstallers(installerCount);
        }

        #if ENABLE_CRIWARE_FILESYSTEM

        private async UniTask UpdateCriAsset(AssetInfo assetInfo, IProgress<DownloadProgressInfo> progress = null, CancellationToken cancelToken = default)
        {
            if (cancelToken.IsCancellationRequested) { return; }

            var criAssetManager = instance.criAssetManager;

            await criAssetManager.UpdateCriAsset(InstallDirectory, assetInfo, progress, cancelToken);
        }

        #endif

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

        private string ConvertCriFilePath(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)){ return null; }
            
            var criFilePath = string.Empty;

            try
            {
                var assetInfo = GetAssetInfo(resourcePath);

                if (assetInfo == null)
                {
                    throw new AssetInfoNotFoundException(resourcePath);
                }

                criFilePath = SimulateMode ? 
                            PathUtility.Combine(UnityPathUtility.GetProjectFolderPath(), externalAssetDirectory, resourcePath) : 
                            GetFilePath(assetInfo);
            }
            catch (AssetInfoNotFoundException e)
            {
                OnError(e);

                return null;
            }

            return criFilePath;
        }

        #endif

        #region Sound

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE
        
        public static async UniTask<CueInfo> GetCueInfo(string resourcePath, string cue)
        {
            return await Instance.GetCueInfoInternal(resourcePath, cue);
        }

        private async UniTask<CueInfo> GetCueInfoInternal(string resourcePath, string cue)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                Debug.LogError("resourcePath empty.");

                return null;
            }

            AssetInfo assetInfo = null;

            try
            {
                assetInfo = GetAssetInfo(resourcePath);

                if (assetInfo == null)
                {
                    throw new AssetInfoNotFoundException(resourcePath);
                }
            }
            catch (AssetInfoNotFoundException e)
            {
                OnError(e);

                return null;
            }

            CueInfo cueInfo = null;

            //===============================================
            // ACB.
            //===============================================
            {
                await InstallCriAsset(assetInfo);

                var criFilePath = ConvertCriFilePath(resourcePath);

                var filePath = PathUtility.GetPathWithoutExtension(criFilePath) + CriAssetDefinition.AcbExtension;

                cueInfo = new CueInfo(filePath, resourcePath, cue);

                if (onLoadAsset != null)
                {
                    onLoadAsset.OnNext(resourcePath);
                }
            }

            //===============================================
            // AWB.
            //===============================================
            {
                // Awbがある場合はそれもロードした扱い.

                var awbResourcePath = Path.ChangeExtension(resourcePath, CriAssetDefinition.AwbExtension);

                var awbAssetInfo = GetAssetInfo(awbResourcePath);

                if (awbAssetInfo != null)
                {
                    await InstallCriAsset(awbAssetInfo);

                    if (onLoadAsset != null)
                    {
                        onLoadAsset.OnNext(awbResourcePath);
                    }
                }
            }
            
            return cueInfo;
        }

        #endif

        #endregion

        #region Movie

        #if ENABLE_CRIWARE_SOFDEC

        public static async UniTask<ManaInfo> GetMovieInfo(string resourcePath)
        {
            return await Instance.GetMovieInfoInternal(resourcePath);
        }

        private async UniTask<ManaInfo> GetMovieInfoInternal(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                Debug.LogError("resourcePath empty.");

                return null;
            }

            AssetInfo assetInfo = null;

            try
            {
                assetInfo = GetAssetInfo(resourcePath);

                if (assetInfo == null)
                {
                    throw new AssetInfoNotFoundException(resourcePath);
                }
            }
            catch (AssetInfoNotFoundException e)
            {
                OnError(e);

                return null;
            }

            await InstallCriAsset(assetInfo);

            var criFilePath = ConvertCriFilePath(resourcePath);

            var filePath = PathUtility.GetPathWithoutExtension(criFilePath) + CriAssetDefinition.UsmExtension;

            var movieInfo = new ManaInfo(filePath);

            if (onLoadAsset != null)
            {
                onLoadAsset.OnNext(resourcePath);
            }

            return movieInfo;
        }

        #endif

        #endregion

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_ADX_LE || ENABLE_CRIWARE_SOFDEC

        private async UniTask InstallCriAsset(AssetInfo assetInfo)
        {
            if (assetInfo == null){ return; }

            if (LocalMode || SimulateMode) { return; }

            // インストール実行中の場合は待つ.

            try
            {
                await criAssetManager.WaitQueueingInstall(assetInfo, cancelSource.Token);
            }
            catch (OperationCanceledException)
            {
                /* Canceled */
            }

            if (cancelSource.IsCancellationRequested){ return; }

            // 更新が必要か確認.

            var requireUpdate = IsRequireUpdate(assetInfo);

            if (!requireUpdate){ return; }

            // 更新.

            var resourcePath = assetInfo.ResourcePath;

            var assetPath = PathUtility.Combine(externalAssetDirectory, resourcePath);

            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                await UpdateAsset(resourcePath, cancelToken: cancelSource.Token);
            }
            catch (OperationCanceledException)
            {
                /* Canceled */
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            if (cancelSource.IsCancellationRequested){ return; }

            sw.Stop();

            if (LogEnable && UnityConsole.Enable)
            {
                var filePath = ConvertCriFilePath(resourcePath);

                var builder = new StringBuilder();

                builder.AppendFormat("Update: {0} ({1:F2}ms)", Path.GetFileName(filePath), sw.Elapsed.TotalMilliseconds).AppendLine();
                builder.AppendLine();
                builder.AppendFormat("LoadPath = {0}", assetPath).AppendLine();
                builder.AppendFormat("FileName = {0}", assetInfo.FileName).AppendLine();

                if (!string.IsNullOrEmpty(assetInfo.Hash))
                {
                    builder.AppendFormat("Hash = {0}", assetInfo.Hash).AppendLine();
                }

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
            }
        }

        #endif
    }
}

#endif
