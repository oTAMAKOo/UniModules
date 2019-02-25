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

        /// <summary> 受信したデータを取り扱う拡張クラス. </summary>
        public DownloadHandler DownloadHandler { get; set; }
        
        /// <summary> 通信中か. </summary>
        public bool Connecting { get; private set; }
        
        /// <summary> タイムアウト時間(秒). </summary>
        public virtual int TimeOutSeconds { get { return 3;  } }
        
        /// <summary> リトライ回数. </summary>
        public virtual int RetryCount { get { return 3; } }

        //----- method -----

        public virtual void Initialize(string url, DataFormat format = DataFormat.MessagePack)
        {
            BaseUrl = url;
            Format = format;

            Headers = new Dictionary<string, string>();
            UrlParams = new Dictionary<string, object>();

            DownloadHandler = new DownloadHandlerBuffer();

            Connecting = false;
        }

        public IObservable<TResult> Get<TResult>(IProgress<float> progress = null) where TResult : class
        {
            BuildUnityWebRequest();

            return SendRequest<TResult>(progress);
        }

        public IObservable<TResult> Post<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class
        {
            BuildUnityWebRequest(content);

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

        private void BuildUnityWebRequest()
        {
            request = new UnityWebRequest(BaseUrl);

            request.method = UnityWebRequest.kHttpVerbGET;
            request.downloadHandler = DownloadHandler;
            request.timeout = TimeOutSeconds;

            BuildUrlParams();
            BuildHeader();
        }

        private void BuildUnityWebRequest<T>(T content)
        {
            request = new UnityWebRequest(BaseUrl);

            request.method = UnityWebRequest.kHttpVerbPOST;
            request.downloadHandler = DownloadHandler;

            BuildUrlParams();
            BuildHeader();
            BuildContent(content);
        }

        protected virtual void RegisterDefaultUrlParams() { }

        private void BuildUrlParams()
        {
            RegisterDefaultUrlParams();

            var urlParams = string.Empty;

            foreach (var urlParam in UrlParams)
            {
                urlParams += string.IsNullOrEmpty(urlParams) ? "?" : "&";

                urlParams += string.Format("{0}={1}", urlParam.Key, urlParam.Value);
            }

            request.url = string.Format("{0}{1}", BaseUrl, urlParams);
        }

        protected virtual void RegisterDefaultHeader() { }

        private void BuildHeader()
        {
            RegisterDefaultHeader();

            foreach (var header in Headers)
            {
                var key = header.Key.TrimEnd('\0');
                var value = header.Value.TrimEnd('\0');

                request.SetRequestHeader(key, value);
            }
        }

        private void BuildContent<T>(T value)
        {
            if (value == null) { return; }

            byte[] bodyData = null;

            switch (Format)
            {
                case DataFormat.Json:
                    var json = JsonFx.Json.JsonWriter.Serialize(value);
                    bodyData = Encoding.UTF8.GetBytes(json);
                    break;

                case DataFormat.MessagePack:
                    MessagePackValidater.ValidateAttribute(typeof(T));
                    bodyData = MessagePackSerializer.Serialize(value, UnityContractResolver.Instance);
                    break;
            }

            bodyData = Encrypt(bodyData);

            request.uploadHandler = new UploadHandlerRaw(bodyData);
        }

        protected virtual byte[] Encrypt(byte[] bytes) { return bytes; }
        protected virtual byte[] Decrypt(byte[] bytes) { return bytes; }
    }
}
