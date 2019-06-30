
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Extensions;

namespace Modules.AssetBundles
{
    public sealed class AssetBundleDownloadHandler : DownloadHandlerScript
    {
        //----- params -----

        //----- field -----

        private FileStream fs = null;
        private int offset = 0;
        private int length = 0;

        //----- property -----

        //----- method -----

        public AssetBundleDownloadHandler(string path, byte[] buffer) : base(buffer)
        {
            fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        }

        // データを受信すると呼び出される.
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            fs.Write(data, 0, dataLength);
            offset += dataLength;

            return true;
        }
        // ダウンロードが終わった時に呼び出される.
        protected override void CompleteContent()
        {
            fs.Flush();
            fs.Close();
        }
        // ダウンロードするサイズ.
        protected override void ReceiveContentLength(int contentLength)
        {
            length = contentLength;
        }

        // downloadProgressの値.
        protected override float GetProgress()
        {
            if (length == 0) { return 0.0f; }

            return (float)offset / length;
        }
    }
}
