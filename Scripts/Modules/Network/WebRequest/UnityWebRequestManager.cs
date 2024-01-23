
using UnityEngine;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniRx;
using Extensions;
using MessagePack;
using MessagePack.Resolvers;
using Modules.Devkit.Console;
using Modules.MessagePack;

namespace Modules.Net.WebRequest
{
    public abstract class UnityWebRequestManager<TInstance, TWebRequest> : WebRequestManager<TInstance, TWebRequest>
        where TInstance : UnityWebRequestManager<TInstance, TWebRequest> 
        where TWebRequest : class, IWebRequestClient, IDisposable, new()
    {
        //----- params -----

        protected static readonly string ConsoleEventName = "API";
        protected static readonly Color ConsoleEventColor = new Color(1f, 0.9f, 0f);

        //----- field -----

        //----- property -----

        /// <summary> ログ出力が有効か. </summary>
        public bool LogEnable { get; protected set; }

        //----- method -----

        public override void Initialize(string hostUrl, DataFormat format = DataFormat.MessagePack, int retryCount = 3, float retryDelaySeconds = 2)
        {
            base.Initialize(hostUrl, format, retryCount, retryDelaySeconds);

			NetworkConnection.OnNotReachableAsObservable()
				.Subscribe()
				.AddTo(Disposable);

			#if UNITY_EDITOR

			ApiTracker.Instance.SetServerUrl(hostUrl);

            #endif
        }

		protected override async Task WaitNetworkReachable(CancellationToken cancelToken)
		{
			await NetworkConnection.WaitNetworkReachable(cancelToken);
		}

		protected override void OnStart(TWebRequest webRequest)
        {
            #if UNITY_EDITOR

            ApiTracker.Instance.Start(webRequest);

            #endif
        }

        protected override void OnCancel()
        {
            #if UNITY_EDITOR

            ApiTracker.Instance.OnForceCancelAll();

            #endif
        }

        /// <summary> 成功時イベント. </summary>
        protected override void OnComplete<TResult>(TWebRequest webRequest, TResult result, double totalMilliseconds)
        {
            if (LogEnable)
            {
                var json = string.Empty;

                switch (Format)
                {
                    case DataFormat.Json:
                        {
                            json = result.ToJson();
                        }
                        break;

                    case DataFormat.MessagePack:
                        {
                            var options = StandardResolverAllowPrivate.Options.WithResolver(UnityCustomResolver.Instance);

                            if (CompressResponseData == DataCompressType.MessagePackLZ4)
                            {
                                options = options.WithCompression(MessagePackCompression.Lz4Block);
                            }

                            json = MessagePackSerializer.SerializeToJson(result, options);
                        }
                        break;
                }

                var builder = new StringBuilder();

                builder.AppendFormat("{0} ({1:F1}ms)", webRequest.HostUrl.Replace(HostUrl, string.Empty), totalMilliseconds).AppendLine();
                builder.AppendLine();
                builder.AppendFormat("URL: {0}", webRequest.Url).AppendLine();
                builder.AppendFormat("Result: {0}", json).AppendLine();
                builder.AppendLine();

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());

                #if UNITY_EDITOR

                ApiTracker.Instance.OnComplete(webRequest, json, totalMilliseconds);

                #endif
            }
        }

        protected override void OnRetry(TWebRequest webRequest)
        {
            #if UNITY_EDITOR

            ApiTracker.Instance.OnRetry(webRequest);

            #endif
        }

        protected override void OnRetryLimit(TWebRequest webRequest)
        {
            #if UNITY_EDITOR

            ApiTracker.Instance.OnRetryLimit(webRequest);

            #endif
        }

        protected override void OnError(TWebRequest webRequest)
        {
            #if UNITY_EDITOR

            ApiTracker.Instance.OnError(webRequest);

            #endif
        }
    }
}
