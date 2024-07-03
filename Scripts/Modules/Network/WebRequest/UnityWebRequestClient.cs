
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Extensions;
using MessagePack;
using MessagePack.Resolvers;
using Modules.MessagePack;
using Modules.Devkit.Console;

using static Extensions.CompressionExtensions;

namespace Modules.Net.WebRequest
{
    public abstract class UnityWebRequestClient : IDisposable, IWebRequestClient
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

        /// <summary> 送信データの圧縮. </summary>
        public DataCompressType CompressRequestData { get; private set; }

        /// <summary> 受信データの圧縮. </summary>
        public DataCompressType CompressResponseData { get; private set; }

        /// <summary> 通信データフォーマット. </summary>
        public DataFormat Format { get; private set; }

        /// <summary> サーバーへ送信するデータを扱うオブジェクト. </summary>
        public UploadHandler UploadHandler { get; private set; }

        /// <summary> サーバーから受信するデータを扱うオブジェクト. </summary>
        public DownloadHandler DownloadHandler { get; private set; }

        /// <summary> タイムアウト時間(秒). </summary>
        public virtual int TimeOutSeconds { get { return 10; } }

        /// <summary> ステータスコード. </summary>
        public string StatusCode
        {
            get { return request.GetResponseHeaders().GetValueOrDefault("STATUS"); }
        }

        /// <summary> 通信中か. </summary>
        public bool IsConnecting { get; private set; }

        /// <summary> キャンセルされたか. </summary>
        public bool IsCanceled { get; private set; }

        /// <summary> 発生したエラー. </summary>
        public Exception Error { get; private set; }

        public bool IsDisposed { get; private set; }

        //----- method -----

        public virtual void Initialize(string hostUrl, DataFormat format = DataFormat.MessagePack)
        {
            HostUrl = hostUrl;
            Format = format;

            Headers = new Dictionary<string, Tuple<bool, string>>();
            UrlParams = new Dictionary<string, object>();

            DownloadHandler = CreateDownloadHandler();

            IsConnecting = false;
            IsCanceled = false;
        }

        ~UnityWebRequestClient()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (IsDisposed){ return; }

            IsDisposed = true;

            if (request !=null)
            {
                request.Dispose();
            }

            GC.SuppressFinalize(this);
        }

        public void SetRequestDataCompress(DataCompressType compressType)
        {
            CompressRequestData = compressType;
        }

        public void SetResponseDataCompress(DataCompressType compressType)
        {
            CompressResponseData = compressType;
        }

        public static void SetCryptoKey(AesCryptoKey cryptoKey)
        {
            UnityWebRequestClient.cryptoKey = cryptoKey;
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

        public Func<CancellationToken, Task<TResult>> Get<TResult>(IProgress<float> progress = null) where TResult : class
        {
            return async token =>
            {
                CreateWebRequest(UnityWebRequest.kHttpVerbGET);

                request.downloadHandler = CreateDownloadHandler();

                return await SendRequest<TResult>(progress, token);
            };
        }

        public Func<CancellationToken, Task<TResult>> Post<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class
        {
            return async token =>
            {
                CreateWebRequest(UnityWebRequest.kHttpVerbPOST);

                request.uploadHandler = CreateUploadHandler(content);
                request.downloadHandler = CreateDownloadHandler();

                return await SendRequest<TResult>(progress, token);
            };
        }

        public Func<CancellationToken, Task<TResult>> Put<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class
        {
            return async token =>
            {
                CreateWebRequest(UnityWebRequest.kHttpVerbPUT);

                request.uploadHandler = CreateUploadHandler(content);
                request.downloadHandler = CreateDownloadHandler();

                return await SendRequest<TResult>(progress, token);
            };
        }

        public Func<CancellationToken, Task<TResult>> Patch<TResult, TContent>(TContent content, IProgress<float> progress = null) where TResult : class
        {
            const string kHttpVerbPatch = "PATCH";

            return async token =>
            {
                CreateWebRequest(kHttpVerbPatch);

                request.uploadHandler = CreateUploadHandler(content);
                request.downloadHandler = CreateDownloadHandler();

                return await SendRequest<TResult>(progress, token);
            };
        }

        public Func<CancellationToken, Task<TResult>> Delete<TResult>(IProgress<float> progress = null) where TResult : class
        {
            return async token =>
            {
                CreateWebRequest(UnityWebRequest.kHttpVerbDELETE);

                request.downloadHandler = CreateDownloadHandler();

                return await SendRequest<TResult>(progress, token);
            };
        }

        public void Cancel(bool throwException = false)
        {
            request.Abort();

            if (throwException)
            {
                Error = new UnityWebRequestErrorException(request);

                throw Error;
            }
        }

        private async Task<TResult> SendRequest<TResult>(IProgress<float> progress, CancellationToken token) where TResult : class
        {
            TResult result = null;

            IsConnecting = true;

            try
            {
                var bytes = await request.Send(progress, token);

                if (bytes != null )
                {
                    result = ReceiveResponse<TResult>(bytes);
                }

                IsConnecting = false;
            }
            catch (OperationCanceledException)
            {
                request.Abort();
            }
            catch (Exception e)
            {
                Error = e;
            }

            return result;
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
                        switch (CompressResponseData)
                        {
                            case DataCompressType.GZip:
                                value = value.Decompress(CompressionAlgorithm.GZip);
                                break;

                            case DataCompressType.Deflate:
                                value = value.Decompress(CompressionAlgorithm.Deflate);
                                break;
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
                            var options = StandardResolverAllowPrivate.Options.WithResolver(UnityCustomResolver.Instance);

                            switch (CompressResponseData)
                            {
                                case DataCompressType.GZip:
                                    value = value.Decompress(CompressionAlgorithm.GZip);
                                    break;

                                case DataCompressType.Deflate:
                                    value = value.Decompress(CompressionAlgorithm.Deflate);
                                    break;

                                case DataCompressType.MessagePackLZ4:
                                    options = options.WithCompression(MessagePackCompression.Lz4Block);
                                    break;
                            }

                            try
                            {
                                result = MessagePackSerializer.Deserialize<TResult>(value, options);
                            }
                            catch
                            {
                                var json = MessagePackSerializer.ConvertToJson(value, options);

                                UnityConsole.Info($"MessagePack Deserialize Failed.\n\n{json}", LogType.Error);

                                throw;
                            }
                        }
                    }
                    break;
            }

            return result;
        }
        
        protected virtual Uri BuildUri()
        {
            RegisterDefaultUrlParams();

            var queryBuilder = new StringBuilder();

            for (var i = 0; i < UrlParams.Count; i++)
            {
                var item = UrlParams.ElementAt(i);

                queryBuilder.Append(i == 0 ? "?" : "&");

                var query = $"{item.Key}={item.Value}";
                
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
                var value = string.IsNullOrEmpty(header.Value.Item2) ? string.Empty : header.Value.Item2.TrimEnd('\0');

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

                        switch (CompressRequestData)
                        {
                            case DataCompressType.GZip:
                                bytes = bytes.Compress(CompressionAlgorithm.GZip);
                                break;

                            case DataCompressType.Deflate:
                                bytes = bytes.Compress(CompressionAlgorithm.Deflate);
                                break;
                        }
                    }
                    break;

                case DataFormat.MessagePack:
                    {
                        var options = StandardResolverAllowPrivate.Options.WithResolver(UnityCustomResolver.Instance);

                        if (CompressRequestData == DataCompressType.MessagePackLZ4)
                        {
                            options = options.WithCompression(MessagePackCompression.Lz4Block);
                        }

                        bytes = MessagePackSerializer.Serialize(content, options);

                        switch (CompressRequestData)
                        {
                            case DataCompressType.GZip:
                                bytes = bytes.Compress(CompressionAlgorithm.GZip);
                                break;

                            case DataCompressType.Deflate:
                                bytes = bytes.Compress(CompressionAlgorithm.Deflate);
                                break;
                        }
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
                        switch (CompressResponseData)
                        {
                            case DataCompressType.GZip:
                                bytes = bytes.Decompress(CompressionAlgorithm.GZip);
                                break;

                            case DataCompressType.Deflate:
                                bytes = bytes.Decompress(CompressionAlgorithm.Deflate);
                                break;
                        }

                        json = Encoding.UTF8.GetString(bytes);
                    }
                    break;

                case DataFormat.MessagePack:
                    {
                        var options = StandardResolverAllowPrivate.Options.WithResolver(UnityCustomResolver.Instance);

                        switch (CompressResponseData)
                        {
                            case DataCompressType.GZip:
                                bytes = bytes.Decompress(CompressionAlgorithm.GZip);
                                break;

                            case DataCompressType.Deflate:
                                bytes = bytes.Decompress(CompressionAlgorithm.Deflate);
                                break;

                            case DataCompressType.MessagePackLZ4:
                                options = options.WithCompression(MessagePackCompression.Lz4Block);
                                break;
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
