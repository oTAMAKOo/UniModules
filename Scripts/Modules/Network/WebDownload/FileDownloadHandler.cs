﻿
using UnityEngine.Networking;
using System.IO;

namespace Modules.Net.WebDownload
{
    public sealed class FileDownloadHandler : DownloadHandlerScript
    {
        //----- params -----

        //----- field -----

        private FileStream fileStream = null;
        private int offset = 0;
        private ulong length = 0;

        //----- property -----

        //----- method -----

        public FileDownloadHandler(string path, byte[] buffer) : base(buffer)
        {
            fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        }

        // データを受信すると呼び出される
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            fileStream.Write(data, 0, dataLength);
            offset += dataLength;

            return true;
        }

        // ダウンロードが終わった時に呼び出される
        protected override void CompleteContent()
        {
            fileStream.Flush();
            fileStream.Close();
        }

        // ダウンロードするサイズ
        protected override void ReceiveContentLengthHeader(ulong contentLength)
        {
            length = contentLength;
        }

        // downloadProgressの値
        protected override float GetProgress()
        {
            if (length == 0){ return 0.0f; }

            return (float)offset / length;
        }
    }
}
