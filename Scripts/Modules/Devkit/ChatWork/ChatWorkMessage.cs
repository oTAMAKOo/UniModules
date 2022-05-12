
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Modules.Devkit.ChatWork
{
    public sealed class ChatWorkMessage
    {
        //----- params -----

        //----- field -----

        private string apiToken = null;

        private ulong roomId = 0;

        //----- property -----

        //----- method -----

        public ChatWorkMessage(string apiToken, ulong roomId)
        {
            this.apiToken = apiToken;
            this.roomId = roomId;
        }

        public async Task<string> SendMessage(string message, bool selfUnRead = false)
        {
            var requestMessage = CreateRequestMessage();

            var requestUrl = GetRequestUrl();

            // 送信情報作成.

            requestUrl += $"messages?body={message}&self_unread={(selfUnRead ? 1 : 0)}";

            requestMessage.RequestUri = new Uri(requestUrl);
            
            // 送信.

            return await SendRequest(requestMessage);
        }

        public async Task<string> SendFile(string filePath, string displayName = null, string message = null)
        {
            if (!File.Exists(filePath)){ return null; }

            // ファイル読み込み.

            var fileBytes = Array.Empty<byte>();

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var binaryReader = new BinaryReader(fileStream))
                {
                    fileBytes = binaryReader.ReadBytes((int)fileStream.Length);
                }
            }

            var fileString = Convert.ToBase64String(fileBytes);

            // 送信情報作成.

            var requestMessage = CreateRequestMessage();

            var requestUrl = GetRequestUrl();

            bool selfUnRead = false;

            requestUrl += $"files&self_unread={(selfUnRead ? 1 : 0)}";

            requestMessage.RequestUri = new Uri(requestUrl);

            var content = new MultipartFormDataContent();

            // メッセージ.
            if (!string.IsNullOrEmpty(message))
            {
                var stringContent = new StringContent(message)
                {
                    Headers =
                    {
                        ContentDisposition = new ContentDispositionHeaderValue("form-data")
                        {
                            Name = "message",
                        }
                    }
                };

                content.Add(stringContent);
            }

            // ファイル.

            if (File.Exists(filePath))
            {
                if (string.IsNullOrEmpty(displayName))
                {
                    displayName = Path.GetFileName(filePath);
                }

                var fileContent = new StringContent($"data:application/octet-stream;name={displayName};base64,{fileString}")
                {
                    Headers =
                    {
                        ContentType = new MediaTypeHeaderValue("application/octet-stream"),
                        ContentDisposition = new ContentDispositionHeaderValue("form-data")
                        {
                            Name = "file",
                            FileName = displayName,
                        }
                    }
                };

                content.Add(fileContent);
            }

            // 送信.

            return await SendRequest(requestMessage);
        }

        private string GetRequestUrl()
        {
            return $"https://api.chatwork.com/v2/rooms/{roomId}/";
        }

        private HttpRequestMessage CreateRequestMessage()
        {
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Headers =
                {
                    { "Accept", "application/json" },
                    { "X-ChatWorkToken", apiToken },
                },
            };

            return requestMessage;
        }

        private async Task<string> SendRequest(HttpRequestMessage requestMessage)
        {
            var result = string.Empty;

            using (var client = new HttpClient())
            {
                using (var response = await client.SendAsync(requestMessage))
                {
                    response.EnsureSuccessStatusCode();

                    result = await response.Content.ReadAsStringAsync();
                }
            }

            return result;
        }
    }
}
