﻿
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UniRx;
using Newtonsoft.Json;
using Extensions;
using MessagePack;
using MessagePack.Resolvers;
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

        protected bool encryptHeader = true;
        protected bool encryptUriQuery = true;
        protected bool encryptBody = true;
        protected bool decryptResponse = true;

        protected static AesCryptoKey cryptoKey = null;

        //----- property -----

        /// <summary> リクエストタイプ. </summary>
        public string Method { get { return request.method; } }

        /// <summary> URL. </summary>
        public string HostUrl { get; private set; }

        /// <summary> リクエストURL. </summary>
        public string Url { get; private set; }

        /// <summary> ヘッダー情報. </summary>
        public IDictionary<string, Tuple<bool, string>> Headers { get; private set; }
        
        /// <summary> URLパラメータ. </summary>
        public IDictionary<string, object> UrlParams { get; private set; }

        /// <summary> 送受信データの圧縮. </summary>
        public bool Compress { get; private set; }

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

        /// <summary> ステータスコード. </summary>
        public string StatusCode
        {
            get { return request.GetResponseHeaders().GetValueOrDefault("STATUS"); }
        }

        //----- method -----

        public virtual void Initialize(string hostUrl, bool compress, DataFormat format = DataFormat.MessagePack)
        {
            HostUrl = hostUrl;
            Compress = compress;
            Format = format;

            Headers = new Dictionary<string, Tuple<bool, string>>();
            UrlParams = new Dictionary<string, object>();

            DownloadHandler = CreateDownloadHandler();

            Connecting = false;
        }

        public static void SetCryptoKey(AesCryptoKey cryptoKey)
        {
            WebRequest.cryptoKey = cryptoKey;
        }

        protected virtual void CreateWebRequest(string method)
        {
            var uri = BuildUri();

            request = new UnityWebRequest(uri);

            request.method = method;
            request.timeout = TimeOutSeconds;

            SetRequestHeaders();

            Url = request.url;
        }

        public IObservable<TResult> Get<TResult>(IProgress<float> progress = null) where TResult : class
        {
            CreateWebRequest(UnityWebRequest.kHttpVerbGET);

            request.downloadHandler = CreateDownloadHandler();

            return SendRequest<TResult>(progress);
        }

        public IObservable<TResult> Post<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class
        {
            CreateWebRequest(UnityWebRequest.kHttpVerbPOST);

            request.uploadHandler = CreateUploadHandler(content);
            request.downloadHandler = CreateDownloadHandler();

            return SendRequest<TResult>(progress);
        }

        public IObservable<TResult> Put<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class
        {
            CreateWebRequest(UnityWebRequest.kHttpVerbPUT);

            request.uploadHandler = CreateUploadHandler(content);
            request.downloadHandler = CreateDownloadHandler();

            return SendRequest<TResult>(progress);
        }

        public IObservable<TResult> Patch<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class
        {
            const string kHttpVerbPatch = "PATCH";

            CreateWebRequest(kHttpVerbPatch);

            request.uploadHandler = CreateUploadHandler(content);
            request.downloadHandler = CreateDownloadHandler();

            return SendRequest<TResult>(progress);
        }

        public IObservable<TResult> Delete<TResult>(IProgress<float> progress = null) where TResult : class
        {
            CreateWebRequest(UnityWebRequest.kHttpVerbDELETE);

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

            return request.Send(progress).Select(x => ReceiveResponse<TResult>(x)).Do(x => Connecting = false);
        }

        private TResult ReceiveResponse<TResult>(byte[] value) where TResult : class
        {
            if (value == null || value.IsEmpty()) { return null; }

            TResult result = null;

            if (decryptResponse)
            {
                value = value.Decrypt(cryptoKey);
            }

            switch (Format)
            {
                case DataFormat.Json:
                    {
                        if (Compress)
                        {
                            value = value.Decompress();
                        }

                        var json = Encoding.UTF8.GetString(value);

                        if (!string.IsNullOrEmpty(json))
                        {
                            result = JsonConvert.DeserializeObject<TResult>(json);
                        }
                    }
                    break;

                case DataFormat.MessagePack:
                    {
                        if (value != null && value.Any())
                        {
                            var options = StandardResolverAllowPrivate.Options.WithResolver(UnityContractResolver.Instance);

                            if (Compress)
                            {
                                options = options.WithCompression(MessagePackCompression.Lz4Block);
                            }

                            result = MessagePackSerializer.Deserialize<TResult>(value, options);
                        }
                    }
                    break;
            }

            return result;
        }
        
        private Uri BuildUri()
        {
            RegisterDefaultUrlParams();

            var queryBuilder = new StringBuilder();

            for (var i = 0; i < UrlParams.Count; i++)
            {
                var item = UrlParams.ElementAt(i);

                queryBuilder.Append(i == 0 ? "?" : "&");

                var query = string.Format("{0}={1}", item.Key, item.Value);
                
                if (encryptUriQuery)
                {
                    query = query.Encrypt(cryptoKey);
                }

                var escapeStr = Uri.EscapeDataString(query);

                queryBuilder.Append(escapeStr);
            }

            var uriBuilder = new UriBuilder(HostUrl)
            {
                Query = queryBuilder.ToString(),
            };

            return uriBuilder.Uri;
        }
        
        private void SetRequestHeaders()
        {
            RegisterDefaultHeader();

            foreach (var header in Headers)
            {
                var key = header.Key.TrimEnd('\0');
                var value = header.Value.Item2.TrimEnd('\0');

                request.SetRequestHeader(key, value);
            }
        }

        protected void SetHeader(string key, string value, bool encrypt = false)
        {
            if (encryptHeader && encrypt)
            {
                Headers[key] = Tuple.Create(true, value.Encrypt(cryptoKey));
            }
            else
            {
                Headers[key] = Tuple.Create(false, value);
            }
        }

        protected virtual DownloadHandler CreateDownloadHandler()
        {
            return new DownloadHandlerBuffer();
        }

        protected virtual UploadHandler CreateUploadHandler<TContent>(TContent content)
        {
            if (content == null) { return null; }

            var bytes = new byte[0];
            
            switch (Format)
            {
                case DataFormat.Json:
                    {
                        var json = JsonConvert.SerializeObject(content);

                        bytes = Encoding.UTF8.GetBytes(json);
                        
                        if (Compress)
                        {
                            bytes = bytes.Compress();
                        }
                    }
                    break;

                case DataFormat.MessagePack:
                    {
                        var options = StandardResolverAllowPrivate.Options.WithResolver(UnityContractResolver.Instance);

                        if (Compress)
                        {
                            options = options.WithCompression(MessagePackCompression.Lz4Block);
                        }

                        bytes = MessagePackSerializer.Serialize(content, options);
                    }
                    break;
            }

            if (encryptBody)
            {
                bytes = bytes.Encrypt(cryptoKey);
            }

            return new UploadHandlerRaw(bytes);
        }

        public string GetUrlParamsString()
        {
            var builder = new StringBuilder();

            foreach (var item in UrlParams)
            {
                builder.AppendFormat("{0} = {1}", item.Key, item.Value).AppendLine();
            }

            return builder.ToString();
        }

        public string GetHeaderString()
        {
            var builder = new StringBuilder();

            foreach (var item in Headers)
            {
                var value = item.Value.Item2;

                if(item.Value.Item1)
                {
                    value = value.Decrypt(cryptoKey);
                }

                builder.AppendFormat("{0} = {1}", item.Key, value).AppendLine();
            }

            return builder.ToString();
        }

        public string GetBodyString()
        {
            if (UploadHandler == null){ return null; }

            if (UploadHandler.data == null){ return null; }

            var json = string.Empty;

            var bytes = UploadHandler.data;

            if (encryptBody)
            {
                bytes = bytes.Decrypt(cryptoKey);
            }

            switch (Format)
            {
                case DataFormat.Json:
                    {
                        if (Compress)
                        {
                            bytes = bytes.Decompress();
                        }

                        json = Encoding.UTF8.GetString(bytes);
                    }
                    break;

                case DataFormat.MessagePack:
                    {
                        var options = StandardResolverAllowPrivate.Options.WithResolver(UnityContractResolver.Instance);

                        if (Compress)
                        {
                            options = options.WithCompression(MessagePackCompression.Lz4Block);
                        }

                        json = MessagePackSerializer.ConvertToJson(bytes, options);
                    }
                    break;
            }

            return json;
        }

        /// <summary> 常時付与されるヘッダー情報を登録 </summary>
        protected virtual void RegisterDefaultHeader() { }

        /// <summary> 常時付与されるURLパラメータを登録 </summary>
        protected virtual void RegisterDefaultUrlParams() { }
    }
}
