
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.ExternalAssets
{
    public sealed partial class ExternalAsset
    {
        //----- params -----

        /// <summary> 最大同時ダウンロード数. </summary>
        private const uint FileAssetDefaultInstallerCount = 8;

        //----- field -----
        
        private FileAssetManager fileAssetManager = null;

        //----- property -----

        //----- method -----

        private void InitializeFileAsset()
        {
            // FileAssetManager初期化.

            fileAssetManager = FileAssetManager.CreateInstance();
            fileAssetManager.Initialize(SimulateMode);
            fileAssetManager.SetMaxDownloadCount(FileAssetDefaultInstallerCount);
            fileAssetManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
            fileAssetManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);
        }

        private async UniTask UpdateFileAsset(AssetInfo assetInfo, IProgress<DownloadProgressInfo> progress = null, CancellationToken cancelToken = default)
        {
            if (cancelToken.IsCancellationRequested) { return; }

            await fileAssetManager.UpdateFileAsset(InstallDirectory, assetInfo, progress, cancelToken);
        }

        public void SetFileAssetInstallerCount(uint installerCount)
        {
            fileAssetManager.SetMaxDownloadCount(installerCount);
        }
    }
}