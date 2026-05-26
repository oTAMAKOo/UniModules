
using System;
using System.Security.Cryptography;
using System.Text;

namespace Modules.AssetBundles
{
    /// <summary> アセットバンドルストリーム用の対称変換アルゴリズム集. </summary>
    public static class AssetBundleStreamCipher
    {
        /// <summary> XOR (位置 × 循環シード). </summary>
        public static void Xor(byte[] buffer, int offset, int count, long streamPos, byte[] seed)
        {
            var seedLength = seed.Length;

            for (var i = 0; i < count; i++)
            {
                buffer[offset + i] ^= seed[(streamPos + i) % seedLength];
            }
        }

        /// <summary> ビット反転 (位置非依存). </summary>
        public static void BitInvert(byte[] buffer, int offset, int count, long streamPos)
        {
            for (var i = 0; i < count; i++)
            {
                buffer[offset + i] = (byte)~buffer[offset + i];
            }
        }

        /// <summary> マスターシードと識別子から派生シードを生成 (SHA-256, 32byte). </summary>
        public static byte[] DeriveSeed(byte[] masterSeed, string identifier)
        {
            using (var sha = SHA256.Create())
            {
                var identifierBytes = Encoding.UTF8.GetBytes(identifier);

                var input = new byte[masterSeed.Length + identifierBytes.Length];

                Buffer.BlockCopy(masterSeed, 0, input, 0, masterSeed.Length);
                Buffer.BlockCopy(identifierBytes, 0, input, masterSeed.Length, identifierBytes.Length);

                return sha.ComputeHash(input);
            }
        }
    }
}
