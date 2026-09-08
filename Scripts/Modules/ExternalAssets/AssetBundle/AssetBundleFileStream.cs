
using System;
using System.Buffers;
using System.IO;

namespace Modules.AssetBundles
{
    /// <summary> AssetBundle 復号ストリーム生成デリゲート. </summary>
    /// <param name="baseStream">パッケージファイルの読み込み元ストリーム</param>
    /// <param name="assetBundleName">アセットバンドル名 (Seed 派生等に使用)</param>
    public delegate AssetBundleFileStream AssetBundleFileStreamFactory(Stream baseStream, string assetBundleName);

    /// <summary> アセットバンドルパッケージ用ストリーム変換ラッパ基底. </summary>
    public abstract class AssetBundleFileStream : Stream
    {
        //----- params -----

        //----- field -----

        private readonly Stream baseStream = null;
        private readonly bool leaveOpen = false;
        private readonly object syncLock = new object();

        //----- property -----

        public override bool CanRead { get { return baseStream.CanRead; } }
        public override bool CanWrite { get { return baseStream.CanWrite; } }
        public override bool CanSeek { get { return baseStream.CanSeek; } }
        public override long Length { get { return baseStream.Length; } }

        public override long Position
        {
            get { return baseStream.Position; }
            set { baseStream.Position = value; }
        }

        //----- method -----

        protected AssetBundleFileStream(Stream baseStream, bool leaveOpen = false)
        {
            this.baseStream = baseStream;
            this.leaveOpen = leaveOpen;
        }

        /// <summary> Read/Write 共通の変換処理 (位置依存暗号にも対応). </summary>
        protected abstract void Transform(byte[] buffer, int offset, int count, long streamPos);

        public override int Read(byte[] buffer, int offset, int count)
        {
            lock (syncLock)
            {
                var pos = baseStream.Position;
                var read = baseStream.Read(buffer, offset, count);

                Transform(buffer, offset, read, pos);

                return read;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count <= 0){ return; }

            lock (syncLock)
            {
                var pos = baseStream.Position;

                // 呼び出し元 buffer を破壊しないよう一時バッファに XOR してから書き込む.
                var tmp = ArrayPool<byte>.Shared.Rent(count);

                try
                {
                    Buffer.BlockCopy(buffer, offset, tmp, 0, count);

                    Transform(tmp, 0, count, pos);

                    baseStream.Write(tmp, 0, count);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(tmp);
                }
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return baseStream.Seek(offset, origin);
        }

        public override void Flush()
        {
            baseStream.Flush();
        }

        public override void SetLength(long value)
        {
            baseStream.SetLength(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !leaveOpen)
            {
                baseStream.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    /// <summary> デフォルト実装 (位置非依存ビット反転). </summary>
    public sealed class DefaultAssetBundleFileStream : AssetBundleFileStream
    {
        //----- method -----

        public DefaultAssetBundleFileStream(Stream baseStream, bool leaveOpen = false)
            : base(baseStream, leaveOpen) { }

        protected override void Transform(byte[] buffer, int offset, int count, long streamPos)
        {
            AssetBundleStreamCipher.BitInvert(buffer, offset, count, streamPos);
        }
    }
}
