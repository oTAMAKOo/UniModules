
#if ENABLE_CRIWARE_FILESYSTEM

using UnityEngine;
using System;
using System.Threading;
using System.IO;
using System.Linq;
using CriWare;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Net;
using Modules.ExternalAssets;

namespace Modules.CriWare
{
	public sealed partial class CriAssetManager
	{
		public sealed class CriAssetInstall
		{
			//----- params -----

			//----- field -----

			//----- property -----

			public AssetInfo AssetInfo { get; private set; }

			public CriFsWebInstaller Installer { get; private set; }

			public IObservable<CriAssetInstall> Task { get; private set; }

			//----- method -----

			public CriAssetInstall(string installPath, AssetInfo assetInfo, IProgress<float> progress = null)
			{
				AssetInfo = assetInfo;

				var downloadUrl = Instance.BuildDownloadUrl(assetInfo);
				var filePath = Instance.GetFilePath(installPath, assetInfo);

				var directory = Path.GetDirectoryName(filePath);

				if (!Directory.Exists(directory))
				{
					Directory.CreateDirectory(directory);
				}

				if (File.Exists(filePath))
				{
					File.Delete(filePath);
				}

				Task = ObservableEx.FromUniTask(cancelToken => Install(downloadUrl, filePath, progress, cancelToken))
					.Select(_ => this)
					.Share();
			}

			private CriFsWebInstaller GetInstaller()
			{
				var installers = Instance.installers;
				var numInstallers = Instance.numInstallers;

				CriFsWebInstaller installer = null;

				// 未使用のインストーラを取得.
				installer = installers.FirstOrDefault(x =>
				{
					var statusInfo = x.GetStatusInfo();

					return statusInfo.status != CriFsWebInstaller.Status.Busy;
				});

				if (installer == null)
				{
					// 最大インストーラ数以下でインストーラが足りない時は生成.
					if (installers.Count < numInstallers)
					{
						installer = new CriFsWebInstaller();

						installers.Add(installer);
					}
				}

				if (installer != null)
				{
					installer.Stop();
				}

				return installer;
			}

			private async UniTask Install(string downloadUrl, string filePath, IProgress<float> progress, CancellationToken cancelToken)
			{
				try
				{
					// 同時インストール待ち.

					while (true)
					{
						// 未使用のインストーラを取得.
						Installer = GetInstaller();

						if (Installer != null) { break; }

						await UniTask.NextFrame(cancelToken);
					}

					// ネットワーク接続待ち.

					await NetworkConnection.WaitNetworkReachable(cancelToken);

					if (cancelToken.IsCancellationRequested)
					{
						Installer.Stop();
						return;
					}

					// ダウンロード.

					Installer.Copy(downloadUrl, filePath);

					// ダウンロード待ち.

					CriFsWebInstaller.StatusInfo statusInfo;

					while (true)
					{
						statusInfo = Installer.GetStatusInfo();

						if (progress != null)
						{
							progress.Report((float)statusInfo.receivedSize / statusInfo.contentsSize);
						}

						if (statusInfo.status != CriFsWebInstaller.Status.Busy) { break; }

						await UniTask.NextFrame(cancelToken);
					}

					if (statusInfo.error != CriFsWebInstaller.Error.None)
					{
						throw new Exception($"[Download Error] {AssetInfo.ResourcePath}\n{statusInfo.error}");
					}
				}
				catch (Exception)
				{
					Installer.Stop();
				}
			}
		}
	}
}

#endif