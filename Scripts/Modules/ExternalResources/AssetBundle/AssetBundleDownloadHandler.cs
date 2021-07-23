
using UnityEngine.Networking;
using System.IO;

namespace Modules.AssetBundles
{
    public sealed class AssetBundleDownloadHandler : DownloadHandlerScript
    {
        //----- params -----

        //----- field -----

        private FileStream fs = null;
        private int offset = 0;
        private ulong length = 0;

        //----- property -----

        //----- method -----

        public AssetBundleDownloadHandler(string path, byte[] buffer) : base(buffer)
        {
            fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
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
        protected override void ReceiveContentLengthHeader(ulong contentLength)
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
