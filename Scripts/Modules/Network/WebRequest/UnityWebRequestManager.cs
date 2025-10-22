
using UnityEngine;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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

        public override void SetHostUrl(string hostUrl)
        {
            base.SetHostUrl(hostUrl);

            #if UNITY_EDITOR

            ApiTracker.Instance.SetServerUrl(hostUrl);

            #endif
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
            var resultJson = string.Empty;

            #if UNITY_EDITOR

            if (string.IsNullOrEmpty(resultJson))
            {
                resultJson = ConvertToJson(result);
            }

            ApiTracker.Instance.OnComplete(webRequest, resultJson, totalMilliseconds);

            #endif

            if (LogEnable)
            {
                var headerString = webRequest.GetHeaderString();
                var bodyString = webRequest.GetBodyString();

                if (string.IsNullOrEmpty(resultJson))
                {
                    resultJson = ConvertToJson(result);
                }

                var builder = new StringBuilder();

                builder.AppendFormat("{0} ({1:F1}ms)", webRequest.HostUrl.Replace(HostUrl, string.Empty), totalMilliseconds).AppendLine();
                builder.AppendLine();
                builder.AppendFormat("URL: {0}", webRequest.Url).AppendLine();
                
                if(!string.IsNullOrEmpty(headerString))
                {
                    builder.AppendFormat("Header: {0}", headerString).AppendLine();
                    builder.AppendLine();
                }

                if(!string.IsNullOrEmpty(bodyString))
                {
                    builder.AppendFormat("Body: {0}", bodyString).AppendLine();
                    builder.AppendLine();
                }

                if (!string.IsNullOrEmpty(resultJson))
                {
                    builder.AppendFormat("Result: {0}", resultJson).AppendLine();
                    builder.AppendLine();
                }

                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, builder.ToString());
            }
        }

        private string ConvertToJson<TResult>(TResult result)
        {
            var resultJson = string.Empty;

            switch (Format)
            {
                case DataFormat.Json:
                    {
                        resultJson = result.ToJson();
                    }
                    break;

                case DataFormat.MessagePack:
                    {
                        var options = StandardResolverAllowPrivate.Options.WithResolver(UnityCustomResolver.Instance);

                        if (CompressResponseData == DataCompressType.MessagePackLZ4)
                        {
                            options = options.WithCompression(MessagePackCompression.Lz4Block);
                        }

                        resultJson = MessagePackSerializer.SerializeToJson(result, options);
                    }
                    break;
            }

            return resultJson;
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
