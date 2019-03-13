﻿
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UniRx;
using Extensions;
using MessagePack;
using Modules.MessagePack;

namespace Modules.Networking
{
    public enum DataFormat
    {
        Json,
        MessagePack,
    }

    public abstract class WebRequest
    {
        //----- params -----

        //----- field -----

        protected UnityWebRequest request = null;

        //----- property -----
        
        /// <summary> URL. </summary>
        public string BaseUrl { get; private set; }
        
        /// <summary> リクエストURL. </summary>
        public string Url { get { return request.url; } }
        
        /// <summary> ヘッダー情報. </summary>
        public IDictionary<string, string> Headers { get; private set; }
        
        /// <summary> URLパラメータ. </summary>
        public IDictionary<string, object> UrlParams { get; private set; }
        
        /// <summary> 通信データフォーマット. </summary>
        public DataFormat Format { get; private set; }

        /// <summary> サーバーへ送信するデータを扱うオブジェクト. </summary>
        public UploadHandler UploadHandler { get; private set; }

        /// <summary> サーバーから受信するデータを扱うオブジェクト. </summary>
        public DownloadHandler DownloadHandler { get; private set; }

        /// <summary> タイムアウト時間(秒). </summary>
        public virtual int TimeOutSeconds { get { return 3; } }

        /// <summary> 通信中か. </summary>
        public bool Connecting { get; private set; }
       
        //----- method -----

        public virtual void Initialize(string url, DataFormat format = DataFormat.MessagePack)
        {
            BaseUrl = url;
            Format = format;

            Headers = new Dictionary<string, string>();
            UrlParams = new Dictionary<string, object>();

            DownloadHandler = CreateDownloadHandler();

            Connecting = false;
        }

        protected virtual UnityWebRequest CreateWebRequest(string method)
        {
            var uri = BuildUri();

            var request = new UnityWebRequest(uri, method);

            SetRequestHeaders();

            request.timeout = TimeOutSeconds;

            return request;
        }

        public IObservable<TResult> Get<TResult>(IProgress<float> progress = null) where TResult : class
        {
            request = CreateWebRequest(UnityWebRequest.kHttpVerbGET);

            request.downloadHandler = CreateDownloadHandler();

            return SendRequest<TResult>(progress);
        }

        public IObservable<TResult> Post<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class
        {
            request = CreateWebRequest(UnityWebRequest.kHttpVerbGET);

            request.uploadHandler = CreateUploadHandler(content);
            request.downloadHandler = CreateDownloadHandler();

            return SendRequest<TResult>(progress);
        }

        public IObservable<TResult> Put<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class
        {
            request = CreateWebRequest(UnityWebRequest.kHttpVerbPUT);

            request.uploadHandler = CreateUploadHandler(content);
            request.downloadHandler = CreateDownloadHandler();

            return SendRequest<TResult>(progress);
        }

        public IObservable<TResult> Delete<TResult>(IProgress<float> progress = null) where TResult : class
        {
            request = CreateWebRequest(UnityWebRequest.kHttpVerbDELETE);

            request.downloadHandler = CreateDownloadHandler();

            return SendRequest<TResult>(progress);
        }

        public void Cancel(bool throwException = false)
        {
            request.Abort();

            if (throwException)
            {
                throw new UnityWebRequestErrorException(request);
            }
        }

        private IObservable<TResult> SendRequest<TResult>(IProgress<float> progress) where TResult : class
        {
            Connecting = true;

            return request.Send(progress).Select(x => Deserialize<TResult>(x)).Do(x => Connecting = false);
        }

        private TResult Deserialize<TResult>(byte[] value) where TResult : class
        {
            if (value == null || value.IsEmpty()) { return null; }

            TResult result = null;

            value = Decrypt(value);

            switch (Format)
            {
                case DataFormat.Json:

                    var json = Encoding.UTF8.GetString(value);

                    if (!string.IsNullOrEmpty(json))
                    {
                        result = JsonFx.Json.JsonReader.Deserialize<TResult>(json);
                    }

                    break;

                case DataFormat.MessagePack:

                    MessagePackValidater.ValidateAttribute(typeof(TResult));

                    if (value != null && value.Any())
                    {
                        result = MessagePackSerializer.Deserialize<TResult>(value, UnityContractResolver.Instance);
                    }

                    break;
            }

            return result;
        }
        
        private Uri BuildUri()
        {
            RegisterDefaultUrlParams();

            var url = new StringBuilder(BaseUrl);

            for (var i = 0; i < UrlParams.Count; i++)
            {
                var item = UrlParams.ElementAt(i);

                url.Append(i == 0 ? "?" : "&");

                url.Append(string.Format("{0}={1}", item.Key, item.Value));
            }

            return new Uri(url.ToString());
        }
        
        private void SetRequestHeaders()
        {
            RegisterDefaultHeader();

            foreach (var header in Headers)
            {
                var key = header.Key.TrimEnd('\0');
                var value = header.Value.TrimEnd('\0');

                request.SetRequestHeader(key, value);
            }
        }

        protected virtual DownloadHandler CreateDownloadHandler()
        {
            return new DownloadHandlerBuffer();
        }

        protected virtual UploadHandler CreateUploadHandler<TContent>(TContent content)
        {
            if (content == null) { return null; }

            byte[] bodyData = null;

            switch (Format)
            {
                case DataFormat.Json:
                    var json = JsonFx.Json.JsonWriter.Serialize(content);
                    bodyData = Encoding.UTF8.GetBytes(json);
                    break;

                case DataFormat.MessagePack:
                    MessagePackValidater.ValidateAttribute(typeof(TContent));
                    bodyData = MessagePackSerializer.Serialize(content, UnityContractResolver.Instance);
                    break;
            }

            bodyData = Encrypt(bodyData);

            return new UploadHandlerRaw(bodyData);
        }

        /// <summary> 常時付与されるURLパラメータを登録 </summary>
        protected virtual void RegisterDefaultUrlParams() { }

        /// <summary> 常時付与されるヘッダー情報を登録 </summary>
        protected virtual void RegisterDefaultHeader() { }

        /// <summary> 送信データ暗号化. </summary>
        protected abstract byte[] Encrypt(byte[] bytes);

        /// <summary> 受信データ復号化. </summary>
        protected abstract byte[] Decrypt(byte[] bytes);
    }
}
