
using System;
using System.Collections.Generic;

namespace Modules.Networking
{
    public enum DataFormat
    {
        Json,
        MessagePack,
    }

    public interface IWebRequest
    {
        //----- params -----

        //----- field -----

        //----- property -----

        /// <summary> リクエストタイプ. </summary>
        string Method { get; }

        /// <summary> URL. </summary>
        string HostUrl { get; }

        /// <summary> リクエストURL. </summary>
        string Url { get; }

        /// <summary> ヘッダー情報. </summary>
        IDictionary<string, Tuple<bool, string>> Headers { get; }
        
        /// <summary> URLパラメータ. </summary>
        IDictionary<string, object> UrlParams { get; }

        /// <summary> 送受信データの圧縮. </summary>
        bool Compress { get; }

        /// <summary> 通信データフォーマット. </summary>
        DataFormat Format { get; }

        /// <summary> タイムアウト時間(秒). </summary>
        int TimeOutSeconds { get; }

        /// <summary> 通信中か. </summary>
        bool Connecting { get; }

        /// <summary> ステータスコード. </summary>
        string StatusCode { get; }

        //----- method -----

        void Initialize(string hostUrl, bool compress, DataFormat format = DataFormat.MessagePack);

        IObservable<TResult> Get<TResult>(IProgress<float> progress = null) where TResult : class;

        IObservable<TResult> Post<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class;

        IObservable<TResult> Put<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class;

        IObservable<TResult> Patch<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class;

        IObservable<TResult> Delete<TResult>(IProgress<float> progress = null) where TResult : class;

        void Cancel(bool throwException = false);

        string GetUrlParamsString();

        string GetHeaderString();

        string GetBodyString();
    }
}