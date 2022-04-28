
using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Cysharp.Threading.Tasks;

namespace Modules.Devkit.ChatWork
{
    public sealed class ChatWorkMessage
    {
        //----- params -----

        private enum ContentType
        {
            Message,
            File,
        }

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

        public async UniTask<string> SendMessage(string message)
        {
            var boundary = Environment.TickCount.ToString();

            // 送信データ.

            var content = "body=" + Uri.EscapeDataString(message);
            var bytes = Encoding.ASCII.GetBytes(content);

            // WebRequest作成.

            var webRequest = CreateWebRequest(ContentType.Message, boundary);

            // 送信.

            var messageId = await SendChatWork(webRequest, bytes);

            return messageId;
        }

        public async UniTask<string> SendFile(string filePath, string displayName = null, string message = null)
        {
            var boundary = Environment.TickCount.ToString();

            // 送信データ.

            var fileBytes = new byte[0];

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException();
            }

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                using (var binaryReader = new BinaryReader(fileStream))
                {
                    fileBytes = binaryReader.ReadBytes((int)fileStream.Length);
                }
            }

            if (string.IsNullOrEmpty(displayName))
            {
                displayName = Path.GetFileName(filePath);
            }

            var postData1 = "--------------------------" + boundary + "\r\n" +
                            "Content-Disposition: form-data; name=\"file\"; filename=" + displayName + "\"\r\n" +
                            "Content-Type: image/jpeg\r\n\r\n";
 
            var postData2 = "\r\n--------------------------" + boundary + "\r\n" +
                            "Content-Disposition: form-data; name=\"message\"\r\n\r\n" +
                            message + "\r\n" +
                            "--------------------------" + boundary + "--";
            
            var postData = new List<byte>();

            postData.AddRange(Encoding.UTF8.GetBytes(postData1));
            postData.AddRange(fileBytes);
            postData.AddRange(Encoding.UTF8.GetBytes(postData2));

            var bytes = postData.ToArray();

            // WebRequest作成.

            var webRequest = CreateWebRequest(ContentType.File, boundary);

            // 送信.

            var messageId = await SendChatWork(webRequest, bytes);

            return messageId;
        }

        private HttpWebRequest CreateWebRequest(ContentType contentType, string boundary)
        {
            var content = string.Empty;

            var url = $"https://api.chatwork.com/v2/rooms/{roomId}/";

            switch (contentType)
            {
                case ContentType.Message:
                    url += "messages";
                    content = "application/x-www-form-urlencoded";
                    break;

                case ContentType.File:
                    url += "files";
                    content = "multipart/form-data; boundary=" + boundary;
                    break;
            }

            var webRequest = (HttpWebRequest)WebRequest.Create(url);

            webRequest.Method = "POST";
            webRequest.Headers.Set("X-ChatWorkToken", apiToken);
            webRequest.ContentType = content;

            return webRequest;
        }

        private async UniTask<string> SendChatWork(HttpWebRequest webRequest, byte[] bytes)
        {
            webRequest.ContentLength = bytes.Length;

            // データ送信.

            using (var requestStream = await webRequest.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(bytes, 0, bytes.Length);
            }

            var response = await webRequest.GetResponseAsync();
            
            // 結果取得.

            var result = string.Empty;

            using(var sr = new StreamReader(response.GetResponseStream()))
            {
                result = await sr.ReadToEndAsync();
            }

            return result;
        }
    }
}
