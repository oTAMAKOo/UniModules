
#if ENABLE_PLAYFAB_CSHARP

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.Internal;

namespace Modules.PlayFabCSharp
{
    public class PlayFabHttpEx
    {
        private static readonly HttpClient _client = new HttpClient();

        public static async Task<object> DoGet(string fullUrl)
        {
            await new PlayFabUtil.SynchronizationContextRemover();

            var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

            HttpResponseMessage httpResponse;
            string httpResponseString;
            IEnumerable<string> requestId;
            bool hasReqId = false;

            try
            {
                httpResponse = await _client.GetAsync(fullUrl);
                httpResponseString = await httpResponse.Content.ReadAsStringAsync();
                hasReqId = httpResponse.Headers.TryGetValues("X-RequestId", out requestId);
            }
            catch (HttpRequestException e)
            {
                return new PlayFabError
                {
                    Error = PlayFabErrorCode.ConnectionError,
                    ErrorMessage = e.InnerException.Message
                };
            }
            catch (Exception e)
            {
                return new PlayFabError
                {
                    Error = PlayFabErrorCode.ConnectionError,
                    ErrorMessage = e.Message
                };
            }
            

            if (!httpResponse.IsSuccessStatusCode)
            {
                var error = new PlayFabError();

                if (string.IsNullOrEmpty(httpResponseString))
                {
                    error.HttpCode = (int)httpResponse.StatusCode;
                    error.HttpStatus = httpResponse.StatusCode.ToString();
                    error.RequestId = GetRequestId(hasReqId, requestId);
                    return error;
                }

                PlayFabJsonError errorResult;
                try
                {
                    errorResult = serializer.DeserializeObject<PlayFabJsonError>(httpResponseString);
                }
                catch (Exception e)
                {
                    error.HttpCode = (int)httpResponse.StatusCode;
                    error.HttpStatus = httpResponse.StatusCode.ToString();
                    error.Error = PlayFabErrorCode.JsonParseError;
                    error.ErrorMessage = e.Message;
                    error.RequestId = GetRequestId(hasReqId, requestId); ;
                    return error;
                }

                error.HttpCode = errorResult.code;
                error.HttpStatus = errorResult.status;
                error.Error = (PlayFabErrorCode)errorResult.errorCode;
                error.ErrorMessage = errorResult.errorMessage;
                error.RetryAfterSeconds = errorResult.retryAfterSeconds;

                if (errorResult.errorDetails != null)
                {
                    error.ErrorDetails = new Dictionary<string, string[]>();
                    foreach (var detail in errorResult.errorDetails)
                    {
                        error.ErrorDetails.Add(detail.Key, detail.Value);
                    }
                }

                error.RequestId = GetRequestId(hasReqId, requestId); ;

                return error;
            }

            if (string.IsNullOrEmpty(httpResponseString))
            {
                return new PlayFabError
                {
                    Error = PlayFabErrorCode.Unknown,
                    ErrorMessage = "Internal server error",
                    RequestId = GetRequestId(hasReqId, requestId)
                };
            }

            return httpResponseString;
        }

        public static async Task<object> DoPut(string fullUrl, byte[] bytes, Dictionary<string, string> extraHeaders = null)
        {
            return await DoPutCore(fullUrl, bytes, extraHeaders);
        }

        public static async Task<object> DoPut(string fullUrl, object request, Dictionary<string, string> extraHeaders = null)
        {
            await new PlayFabUtil.SynchronizationContextRemover();

            var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);

            var bodyString = string.Empty;

            if (request == null)
            {
                bodyString = "{}";
            }
            else
            {
                bodyString = serializer.SerializeObject(request);
            }

            var bytes = Encoding.UTF8.GetBytes(bodyString);

            return await DoPutCore(fullUrl, bytes, extraHeaders);
        }

        private static async Task<object> DoPutCore(string fullUrl, byte[] bytes, Dictionary<string, string> extraHeaders = null)
        {
            await new PlayFabUtil.SynchronizationContextRemover();

            var serializer = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
            
            HttpResponseMessage httpResponse;
            string httpResponseString;
            IEnumerable<string> requestId;
            bool hasReqId = false;
            using (var postBody = new ByteArrayContent(bytes))
            {
                postBody.Headers.Add("Content-Type", "application/json");
                postBody.Headers.Add("X-PlayFabSDK", PlayFabSettings.SdkVersionString);
                if (extraHeaders != null)
                {
                    foreach (var headerPair in extraHeaders)
                    {
                        // Special case for Authorization header
                        if (headerPair.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                        {
                            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", headerPair.Value);
                        }
                        else
                        {
                            postBody.Headers.Add(headerPair.Key, headerPair.Value);
                        }
                    }
                }

                try
                {
                    httpResponse = await _client.PutAsync(fullUrl, postBody);
                    httpResponseString = await httpResponse.Content.ReadAsStringAsync();
                    hasReqId = httpResponse.Headers.TryGetValues("X-RequestId", out requestId);
                }
                catch (HttpRequestException e)
                {
                    return new PlayFabError
                    {
                        Error = PlayFabErrorCode.ConnectionError,
                        ErrorMessage = e.InnerException.Message
                    };
                }
                catch (Exception e)
                {
                    return new PlayFabError
                    {
                        Error = PlayFabErrorCode.ConnectionError,
                        ErrorMessage = e.Message
                    };
                }
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                var error = new PlayFabError();

                if (string.IsNullOrEmpty(httpResponseString))
                {
                    error.HttpCode = (int)httpResponse.StatusCode;
                    error.HttpStatus = httpResponse.StatusCode.ToString();
                    error.RequestId = GetRequestId(hasReqId, requestId);
                    return error;
                }

                PlayFabJsonError errorResult;
                try
                {
                    errorResult = serializer.DeserializeObject<PlayFabJsonError>(httpResponseString);
                }
                catch (Exception e)
                {
                    error.HttpCode = (int)httpResponse.StatusCode;
                    error.HttpStatus = httpResponse.StatusCode.ToString();
                    error.Error = PlayFabErrorCode.JsonParseError;
                    error.ErrorMessage = e.Message;
                    error.RequestId = GetRequestId(hasReqId, requestId); ;
                    return error;
                }

                error.HttpCode = errorResult.code;
                error.HttpStatus = errorResult.status;
                error.Error = (PlayFabErrorCode)errorResult.errorCode;
                error.ErrorMessage = errorResult.errorMessage;
                error.RetryAfterSeconds = errorResult.retryAfterSeconds;

                if (errorResult.errorDetails != null)
                {
                    error.ErrorDetails = new Dictionary<string, string[]>();
                    foreach (var detail in errorResult.errorDetails)
                    {
                        error.ErrorDetails.Add(detail.Key, detail.Value);
                    }
                }

                error.RequestId = GetRequestId(hasReqId, requestId); ;

                return error;
            }

            if (string.IsNullOrEmpty(httpResponseString))
            {
                return new PlayFabError
                {
                    Error = PlayFabErrorCode.Unknown,
                    ErrorMessage = "Internal server error",
                    RequestId = GetRequestId(hasReqId, requestId)
                };
            }

            return httpResponseString;
        }

        private static string GetRequestId(bool hasReqId, IEnumerable<string> reqIdContainer)
        {
            const string defaultReqId = "NoRequestIdFound";

            if (!hasReqId)
            {
                return defaultReqId;
            }

            try
            {
                string reqId = reqIdContainer.FirstOrDefault();
                if (string.IsNullOrEmpty(reqId))
                {
                    reqId = defaultReqId;
                }

                return reqId;
            }
            catch (Exception e)
            {
                return "Failed to Enumerate RequestId. Exception message: " + e.Message;
            }
        }
    }
}

#endif