
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC

using UnityEngine;
using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Text;
using UniRx;
using Extensions;
using Modules.CriWare;
using Modules.Devkit.Console;

#if ENABLE_CRIWARE_ADX
using Modules.SoundManagement;
#endif

#if ENABLE_CRIWARE_SOFDEC
using Modules.MovieManagement;
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
            criAssetManager.Initialize(resourceDirectory, MaxDownloadCount, simulateMode);
            criAssetManager.OnTimeOutAsObservable().Subscribe(x => OnTimeout(x)).AddTo(Disposable);
            criAssetManager.OnErrorAsObservable().Subscribe(x => OnError(x)).AddTo(Disposable);
        }

        private bool IsCriAsset(string extension)
        {
            return CriAssetDefinition.AssetAllExtensions.Any(x => x == extension);
        }

        private IObservable<bool> UpdateCriAsset(AssetInfo assetInfo, IProgress<float> progress = null)
        {
            return Observable.FromMicroCoroutine<bool>(observer => UpdateCriAssetInternal(observer, assetInfo, progress));
        }

        private IEnumerator UpdateCriAssetInternal(IObserver<bool> observer, AssetInfo assetInfo, IProgress<float> progress = null)
        {
            var result = true;

            var resourcePath = assetInfo.ResourcePath;

            var filePath = ConvertCriFilePath(resourcePath);

            // ローカルバージョンが最新の場合は更新しない.
            if (!CheckAssetVersion(resourcePath, filePath))
            {
                var updateYield = instance.criAssetManager
                    .UpdateCriAsset(assetInfo, progress)
                    .ToYieldInstruction(false, yieldCancel.Token);

                while (!updateYield.IsDone)
                {
                    yield return null;
                }

                if (updateYield.IsCanceled || updateYield.HasError)
                {
                    result = false;
                }
            }

            observer.OnNext(result);
            observer.OnCompleted();
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

                observer.OnNext(File.Exists(filePath) ? new CueInfo(filePath, resourcePath, cue) : null);

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

                if (File.Exists(filePath))
                {
                    var movieInfo = new ManaInfo(filePath);

                    observer.OnNext(movieInfo);

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
    }
}

#endif
