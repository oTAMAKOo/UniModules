
using System;
using System.IO;
using System.Security.Cryptography;

namespace Modules.AssetBundles
{
    /// <summary> AES-CTR モードのアセットバンドルストリーム </summary>
    /// <remarks> nonce は AssetBundle ごとに変えること.
    /// 同一 key+nonce で別データを暗号化すると CTR の安全性が崩れるため, AssetBundle名から派生させる設計を推奨.
    /// </remarks>
    public abstract class AesCtrAssetBundleFileStream : AssetBundleFileStream
    {
        //----- params -----

        // AES ブロックサイズ (128bit).
        private const int BlockSize = 16;

        // カウンタブロック内訳 (nonce + counter = 16byte).
        private const int NonceSize = 8;
        private const int CounterSize = 8;

        //----- field -----

        private readonly Aes aes = null;
        private readonly ICryptoTransform encryptor = null;
        private readonly byte[] nonce = null;

        // 変換用バッファ (基底クラスの lock 内でのみ使用するため共有して確保).
        private readonly byte[] counterBlock = new byte[BlockSize];
        private readonly byte[] keyStream = new byte[BlockSize];

        //----- method -----

        /// <summary> AES-CTR ストリーム生成. </summary>
        /// <param name="key">AES 鍵 (16/24/32byte)</param>
        /// <param name="nonce">ナンス (8byte). AssetBundle ごとに変えること (同一 key+nonce での暗号化は CTR の安全性を損なう)</param>
        protected AesCtrAssetBundleFileStream(Stream baseStream, byte[] key, byte[] nonce, bool leaveOpen = false)
            : base(baseStream, leaveOpen)
        {
            aes = Aes.Create();
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key = key;

            encryptor = aes.CreateEncryptor();

            this.nonce = nonce;
        }

        protected override void Transform(byte[] buffer, int offset, int count, long streamPos)
        {
            var currentBlock = streamPos / BlockSize;
            var startOffsetInBlock = (int)(streamPos % BlockSize);

            var processed = 0;

            while (processed < count)
            {
                // カウンタブロック構築: nonce(8byte) + counter(8byte, ビッグエンディアン).
                Buffer.BlockCopy(nonce, 0, counterBlock, 0, NonceSize);
                WriteCounter(counterBlock, NonceSize, currentBlock);

                // ECB で 1 ブロック暗号化 → 鍵ストリーム生成.
                encryptor.TransformBlock(counterBlock, 0, BlockSize, keyStream, 0);

                // 鍵ストリームを XOR (CTR は暗号化・復号化が対称).
                var inBlockStart = (processed == 0) ? startOffsetInBlock : 0;
                var blockProcessCount = Math.Min(BlockSize - inBlockStart, count - processed);

                for (var i = 0; i < blockProcessCount; i++)
                {
                    buffer[offset + processed + i] ^= keyStream[inBlockStart + i];
                }

                processed += blockProcessCount;
                currentBlock++;
            }
        }

        /// <summary> カウンタ値をビッグエンディアンで書き込む. </summary>
        private static void WriteCounter(byte[] buffer, int offset, long counter)
        {
            for (var i = CounterSize - 1; i >= 0; i--)
            {
                buffer[offset + i] = (byte)(counter & 0xFF);

                counter >>= 8;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (encryptor != null) { encryptor.Dispose(); }

                if (aes != null) { aes.Dispose(); }
            }

            base.Dispose(disposing);
        }
    }
}
