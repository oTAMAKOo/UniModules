
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Modules.Network.WebRequest
{
    public interface IWebRequestClient
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary> リクエストタイプ. </summary>
        Method Method { get; }

        /// <summary> URL. </summary>
        string HostUrl { get; }

        /// <summary> リクエストURL. </summary>
        string Url { get; }

        /// <summary> ヘッダー情報. </summary>
        IDictionary<string, Tuple<bool, string>> Headers { get; }
        
        /// <summary> URLパラメータ. </summary>
        IDictionary<string, object> UrlParams { get; }

        /// <summary> 送信データの圧縮. </summary>
        DataCompressType CompressRequestData { get; }

        /// <summary> 受信データの圧縮. </summary>
        DataCompressType CompressResponseData { get; }

        /// <summary> 通信データフォーマット. </summary>
        DataFormat Format { get; }

        /// <summary> タイムアウト時間(秒). </summary>
        int TimeOutSeconds { get; }

        /// <summary> ステータスコード (STATUS ヘッダー. 応答ヘッダーが無い場合は null). </summary>
        string StatusCode { get; }

        /// <summary> 通信中か. </summary>
        bool IsConnecting { get; }

        /// <summary> キャンセルされたか. </summary>
        bool IsCanceled { get; }

        /// <summary> 発生したエラー. </summary>
        Exception Error { get; }

        /// <summary> 直近の送信が成功ステータス (<see cref="UnityWebRequestExtensions.IsSuccess"/> が true) の応答を受信し、受信データの変換まで完了したか. </summary>
        bool IsSuccess { get; }

        /// <summary> 直近の送信の HTTP ステータスコード (応答が無い場合は 0). </summary>
        long ResponseCode { get; }

        //----- method -----

        void Initialize(string hostUrl, DataFormat format = DataFormat.MessagePack);

        void SetMethod(Method method);

        void SetRequestDataCompress(DataCompressType compressType);

        void SetResponseDataCompress(DataCompressType compressType);

        Func<CancellationToken, Task<TResult>> Get<TResult>(IProgress<float> progress = null) where TResult : class;

		Func<CancellationToken, Task<TResult>> Post<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class;
		
		Func<CancellationToken, Task<TResult>> Put<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class;
		
		Func<CancellationToken, Task<TResult>> Patch<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class;
		
		Func<CancellationToken, Task<TResult>> Delete<TResult>(IProgress<float> progress = null) where TResult : class;

        void Cancel(bool throwException = false);

        string GetUrlParamsString();

        string GetHeaderString();

        string GetBodyString();
        
        string GetReceiveResponseString();
    }
}