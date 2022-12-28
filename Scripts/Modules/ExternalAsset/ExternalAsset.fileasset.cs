
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
		private const uint FileAssetDefaultInstallerCount = 16;

        //----- field -----
		
		private FileAssetManager fileAssetManager = null;

        //----- property -----

        //----- method -----

		private void InitializeFileAsset()
		{
			// FileAssetManager初期化.

			fileAssetManager = FileAssetManager.CreateInstance();
			fileAssetManager.Initialize(simulateMode);
			fileAssetManager.SetMaxDownloadCount(FileAssetDefaultInstallerCount);
			fileAssetManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
			fileAssetManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);
		}

		private async UniTask UpdateFileAsset(CancellationToken cancelToken, AssetInfo assetInfo, IProgress<float> progress = null)
		{
			// ローカルバージョンが最新の場合は更新しない.
			if (CheckAssetVersion(assetInfo)){ return; }
			
			await fileAssetManager.UpdateFileAsset(InstallDirectory, assetInfo, cancelToken, progress);
		}

		public void SetFileAssetInstallerCount(uint installerCount)
		{
			fileAssetManager.SetMaxDownloadCount(installerCount);
		}
    }
}