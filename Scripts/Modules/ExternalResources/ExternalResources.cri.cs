
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.CriWare;
using Modules.Devkit.Console;

#if ENABLE_CRIWARE_ADX
using Modules.Sound;
#endif

#if ENABLE_CRIWARE_SOFDEC
using Modules.Movie;
#endif

namespace Modules.ExternalResource
{
    public sealed partial class ExternalResources
    {
        //----- params -----

        //----- field -----

        // CriWare管理.
        private CriAssetManager criAssetManager = null;

        //----- property -----

        //----- method -----

        private void InitializeCri()
        {
            // CriAssetManager初期化.

            criAssetManager = CriAssetManager.CreateInstance();
            criAssetManager.Initialize(MaxDownloadCount, simulateMode);
            criAssetManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
            criAssetManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);
        }

        private bool IsCriAsset(string extension)
        {
            return CriAssetDefinition.AssetAllExtensions.Any(x => x == extension);
        }

        private async UniTask<bool> UpdateCriAsset(CancellationToken cancelToken, AssetInfo assetInfo, IProgress<float> progress = null)
        {
			var result = true;

            var resourcePath = assetInfo.ResourcePath;

            var filePath = ConvertCriFilePath(resourcePath);

            // ローカルバージョンが最新の場合は更新しない.
            if (!CheckAssetVersion(resourcePath, filePath))
            {
				try
				{
					var criAssetManager = instance.criAssetManager;

					await criAssetManager.UpdateCriAsset(assetInfo, cancelToken, progress);
				}
				catch
				{
					result = false;
				}
			}

			return result;
        }

        #if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

        private string ConvertCriFilePath(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)){ return null; }

            var assetInfo = GetAssetInfo(resourcePath);

            if (assetInfo == null)
            {
                Debug.LogErrorFormat("AssetInfo not found.\n{0}", resourcePath);

                return null;
            }

            return simulateMode ?
                PathUtility.Combine(new string[] { UnityPathUtility.GetProjectFolderPath(), resourceDirectory, resourcePath }) :
                criAssetManager.GetFilePath(assetInfo);
        }

        #endif

        #region Sound

        #if ENABLE_CRIWARE_ADX
        
        public static async UniTask<CueInfo> GetCueInfo(string resourcePath, string cue)
        {
            return await Instance.GetCueInfoInternal(resourcePath, cue);
        }

        private async UniTask<CueInfo> GetCueInfoInternal(string resourcePath, string cue)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                throw new ArgumentException("resourcePath empty.");
            }

            var filePath = ConvertCriFilePath(resourcePath);

            if (!LocalMode && !simulateMode)
            {
                if (!CheckAssetVersion(resourcePath, filePath))
                {
                    var assetPath = PathUtility.Combine(resourceDirectory, resourcePath);

                    var sw = System.Diagnostics.Stopwatch.StartNew();

					try
					{
						await UpdateAsset(resourcePath).AttachExternalCancellation(cancelSource.Token);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}

					sw.Stop();

                    if (LogEnable && UnityConsole.Enable)
                    {
                        var assetInfo = GetAssetInfo(resourcePath);

                        if (assetInfo != null)
                        {
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
                }
            }

            filePath = PathUtility.GetPathWithoutExtension(filePath) + CriAssetDefinition.AcbExtension;

            var cueInfo = File.Exists(filePath) ? new CueInfo(filePath, resourcePath, cue) : null;

            if (onLoadAsset != null)
            {
                onLoadAsset.OnNext(resourcePath);
            }
            
            return cueInfo;
        }

        #endif

        #endregion

        #region Movie

        #if ENABLE_CRIWARE_SOFDEC

        public static async UniTask<ManaInfo> GetMovieInfo(string resourcePath)
        {
            return await Instance.GetMovieInfoInternal( resourcePath);
        }

        private async UniTask<ManaInfo> GetMovieInfoInternal(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                throw new ArgumentException("resourcePath empty.");
            }

			var filePath = ConvertCriFilePath(resourcePath);

            if (!LocalMode && !simulateMode)
            {
                if (!CheckAssetVersion(resourcePath, filePath))
                {
                    var assetPath = PathUtility.Combine(resourceDirectory, resourcePath);

                    var sw = System.Diagnostics.Stopwatch.StartNew();

					try
					{
						await UpdateAsset(resourcePath).AttachExternalCancellation(cancelSource.Token);
					}
					catch (Exception e)
					{
						Debug.LogException(e);
					}

					sw.Stop();

                    if (LogEnable && UnityConsole.Enable)
                    {
                        var assetInfo = GetAssetInfo(resourcePath);

                        if (assetInfo != null)
                        {
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
                }
            }

            filePath = PathUtility.GetPathWithoutExtension(filePath) + CriAssetDefinition.UsmExtension;

			ManaInfo movieInfo = null;

            if (File.Exists(filePath))
            {
                movieInfo = new ManaInfo(filePath);
				
                if (onLoadAsset != null)
                {
                    onLoadAsset.OnNext(resourcePath);
                }
            }
            else
            {
                Debug.LogErrorFormat("File not found.\n{0}", filePath);
			}

            return movieInfo;
        }

        #endif

        #endregion
    }
}

#endif
