
// Reference from: https://stackoverflow.com/questions/5026409/how-to-add-seek-and-position-capabilities-to-cryptostream

using System;
using System.IO;
using System.Security.Cryptography;

namespace Extensions
{
    public sealed class SeekableCryptoStream : Stream
    {
        //----- params -----

        //----- field -----

        private Stream baseStream = null;
        private AesCryptoStreamKey cryptoKey = null;

        //----- property -----

        public bool AutoDisposeBaseStream { get; set; } = true;

        //----- method -----

        public SeekableCryptoStream(Stream baseStream, AesCryptoStreamKey cryptoKey)
        {
            this.baseStream = baseStream;
            this.cryptoKey = cryptoKey;
        }

        private void Cipher(byte[] buffer, int offset, int count, long streamPos)
        {
            var encryptor = cryptoKey.Encryptor;

            var blockSizeInByte = cryptoKey.BlockSize / 8;
            var blockNumber = streamPos / blockSizeInByte + 1;
            var keyPos = streamPos % blockSizeInByte;
            
            var outBuffer = new byte[blockSizeInByte];
            var nonce = new byte[blockSizeInByte];
            var init = false;

            lock (encryptor)
            {
                for (var i = offset; i < count; i++)
                {
                    // ※ nonceを暗号化して次のxorバッファを作成.

                    if (!init || keyPos % blockSizeInByte == 0)
                    {
                        BitConverter.GetBytes(blockNumber).CopyTo(nonce, 0);

                        encryptor.TransformBlock(nonce, 0, nonce.Length, outBuffer, 0);

                        if (init){ keyPos = 0; }
                    
                        init = true;
                    
                        blockNumber++;
                    }
                
                    buffer[i] ^= outBuffer[keyPos];

                    keyPos++;
                }
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var streamPos = Position;

            var ret = baseStream.Read(buffer, offset, count);
            
            Cipher(buffer, offset, count, streamPos);

            return ret;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Cipher(buffer, offset, count, Position);

            baseStream.Write(buffer, offset, count);
        }

        public override bool CanRead { get { return baseStream.CanRead; } }
        
        public override bool CanSeek { get { return baseStream.CanSeek; } }
        
        public override bool CanWrite { get { return baseStream.CanWrite; } }
        
        public override long Length { get { return baseStream.Length; } }
        
        public override long Position { get { return baseStream.Position; } set { baseStream.Position = value; } }
        
        public override void Flush() { baseStream.Flush(); }
        
        public override void SetLength(long value) { baseStream.SetLength(value); }

        public override long Seek(long offset, SeekOrigin origin) { return baseStream.Seek(offset, origin); }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (AutoDisposeBaseStream)
                {
                    baseStream?.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }

    public sealed class AesCryptoStreamKey : AesCryptoKey
    {
        public AesCryptoStreamKey(string password) : base(password, CreateAesManaged()){}

        public AesCryptoStreamKey(string key, string iv) : base(key, iv, CreateAesManaged()){}

        private static AesManaged CreateAesManaged()
        {
            var aesManaged = new AesManaged()
            {
                KeySize = 128,
                Mode = CipherMode.ECB,
                Padding = PaddingMode.None,
            };
            
            return aesManaged;
        }
    }
}