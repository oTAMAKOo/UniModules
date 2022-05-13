
using System;
using System.IO;
using System.Net.Http;
using System.Text;
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
            var requestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Headers =
                {
                    { "Accept", "application/json" },
                    { "X-ChatWorkToken", apiToken },
                },
            };

            var requestUrl = GetRequestUrl() + "messages";

            // 送信情報作成.

            requestUrl += $"?body={message}&self_unread={(selfUnRead ? 1 : 0)}";

            requestMessage.RequestUri = new Uri(requestUrl);
            
            // 送信.

            var result = string.Empty;

            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.SendAsync(requestMessage))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        result = await response.Content.ReadAsStringAsync();
                    }
                }
            }

            return result;
        }

        public async Task<string> SendFile(string filePath, string message = null, string displayName = null)
        {
            if (!File.Exists(filePath)){ return null; }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = Path.GetFileName(filePath);
            }

            var result = string.Empty;

            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add("X-ChatWorkToken", apiToken);

                using (var multipart = new MultipartFormDataContent("---boundary---"))
                {
                    // ファイル.

                    var fileContent = new StreamContent(File.OpenRead(filePath));

                    fileContent.Headers.Add("Content-Disposition", $@"form-data; name=""file""; filename=""{displayName}""");

                    multipart.Add(fileContent);

                    // メッセージ.

                    if (!string.IsNullOrEmpty(message))
                    {
                        var messageContent = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(message)));

                        messageContent.Headers.Add("Content-Disposition", $@"form-data; name=""message""");

                        multipart.Add(messageContent);
                    }

                    // 送信.

                    var requestUrl = GetRequestUrl() + "files";

                    using (var response = await httpClient.PostAsync(requestUrl, multipart))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            result = await response.Content.ReadAsStringAsync();
                        }
                    }
                }
            }

            return result;
        }

        private string GetRequestUrl()
        {
            return $"https://api.chatwork.com/v2/rooms/{roomId}/";
        }
    }
}
