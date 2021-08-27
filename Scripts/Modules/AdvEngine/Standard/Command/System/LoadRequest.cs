
#if ENABLE_MOONSHARP

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Extensions;
using UniRx;
using Modules.ExternalResource;
using MoonSharp.Interpreter;

namespace Modules.AdvKit.Standard
{
    public sealed class LoadRequest : Command
    {
        //----- params -----

        public sealed class Request : LifetimeDisposable
        {
            public bool IsStart { get; private set; }
            public AssetInfo[] AssetInfos { get; private set; }

            public Request(AssetInfo[] assetInfos)
            {
                AssetInfos = assetInfos ?? new AssetInfo[0];
                IsStart = false;
            }

            public void LoadStart()
            {
                IsStart = true;
            }
        }

        //----- field -----

        private IDisposable loadDisposable = null;

        private Subject<Request> onLoadRequest = null;

        //----- property -----

        public override string CommandName { get { return "LoadRequest"; } }

        //----- method -----

        public override object GetCommandDelegate()
        {
            return (Func<DynValue>)CommandFunction;
        }

        private DynValue CommandFunction()
        {
            var returnValue = DynValue.Nil;

            try
            {
                var advEngine = AdvEngine.Instance;

                var requests = advEngine.Resource.GetRequests();

                var resourcePaths = requests.Select(x => x.Key).Distinct().ToArray();

                var assetInfos = new List<AssetInfo>();

                var builder = new StringBuilder();

                builder.Append("Request Files").AppendLine();

                foreach (var resourcePath in resourcePaths)
                {
                    var assetInfo = ExternalResources.Instance.GetAssetInfo(resourcePath);

                    if (assetInfo != null)
                    {
                        assetInfos.Add(assetInfo);

                        builder.AppendFormat("{0} ({1}byte)", assetInfo.ResourcePath, assetInfo.FileSize).AppendLine();
                    }
                    else
                    {
                        Debug.LogErrorFormat("AssetInfo not found. {0}", resourcePath);
                    }
                }

                using (new DisableStackTraceScope())
                {
                    Debug.Log(builder.ToString());
                }

                Action execteLoad = () =>
                {
                    loadDisposable = requests.Select(x => x.Value).WhenAll()
                        .Subscribe(_ => advEngine.Resume())
                        .AddTo(Disposable);
                };

                if (onLoadRequest != null && onLoadRequest.HasObservers)
                {
                    var request = new Request(assetInfos.ToArray());

                    onLoadRequest.OnNext(request);

                    Observable.EveryUpdate().SkipWhile(x => !request.IsStart)
                        .First()
                        .Subscribe(_ => execteLoad())
                        .AddTo(Disposable);
                }
                else
                {
                    execteLoad();
                }

                returnValue = YieldWait;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return returnValue;
        }

        public void Cancel()
        {
            if (loadDisposable != null)
            {
                loadDisposable.Dispose();
                loadDisposable = null;
            }
        }

        public IObservable<Request> OnLoadRequestAsObservable()
        {
            return onLoadRequest ?? (onLoadRequest = new Subject<Request>());
        }
    }
}

#endif
