
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Extensions
{
    public static class AESExtension
    {
        /// <summary>
        /// バイト配列をAESで暗号化.
        /// </summary>
        public static byte[] Encrypt(this byte[] value, AesCryptoKey aesCryptoKey)
        {
            if (value == null || value.IsEmpty()) { return null; }

            byte[] result = null;

            using (var encryptor = aesCryptoKey.AesManaged.CreateEncryptor())
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(value, 0, value.Length);
                        cryptoStream.Close();

                        result = memoryStream.ToArray();
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// バイト配列をAESで復号化.
        /// </summary>
        public static byte[] Decrypt(this byte[] value, AesCryptoKey aesCryptoKey)
        {
            if (value == null || value.IsEmpty()) { return null; }

            byte[] result = null;

            using (var decryptor = aesCryptoKey.AesManaged.CreateDecryptor())
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Write))
                    {
                        cryptoStream.Write(value, 0, value.Length);
                        cryptoStream.Close();

                        result = memoryStream.ToArray();
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// 文字列をAESで暗号化.
        /// </summary>
        public static string Encrypt(this string value, AesCryptoKey aesCryptoKey)
        {
            if (string.IsNullOrEmpty(value)) { return null; }

            string result = null;
            
            using (var encryptor = aesCryptoKey.AesManaged.CreateEncryptor())
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        var toEncrypt = Encoding.UTF8.GetBytes(value);

                        cryptoStream.Write(toEncrypt, 0, toEncrypt.Length);
                        cryptoStream.FlushFinalBlock();

                        var encrypted = memoryStream.ToArray();

                        result = Convert.ToBase64String(encrypted).Replace('+', '-').Replace('/', '_');
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// 文字列をAESで復号化.
        /// </summary>
        public static string Decrypt(this string value, AesCryptoKey aesCryptoKey)
        {
            if (string.IsNullOrEmpty(value)) { return null; }

            string result = null;
            
            var encrypted = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/'));
            var fromEncrypt = new byte[encrypted.Length];

            using (var decryptor = aesCryptoKey.AesManaged.CreateDecryptor())
            {
                using (var memoryStream = new MemoryStream(encrypted))
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    {
                        cryptoStream.Read(fromEncrypt, 0, fromEncrypt.Length);

                        result = Encoding.UTF8.GetString(fromEncrypt);
                    }
                }
            }

            // string.Lengthした際に終端にNull文字が混入する為Null文字を削る.
            return result.TrimEnd('\0');
        }
    }

    public sealed class AesCryptoKey
    {
        //----- params -----
        
        private static readonly byte[] Salt = { 0xe6, 0xdc, 0xff, 0x74, 0xad, 0xad, 0x7a, 0xee, 0xc5, 0xfe, 0x50, 0xaf, 0x4d, 0x08, 0x2d, 0x3c };

        //----- field -----
        
        //----- property -----

        public AesManaged AesManaged { get; private set; }

        //----- method -----

        public AesCryptoKey(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Require password string.");
            }

            // 疑似乱数を使用してパスワードを暗号化.
            var pdb = new Rfc2898DeriveBytes(password, Salt, 64);

            AesManaged = CreateAesManaged();

            AesManaged.Key = pdb.GetBytes(32);
            AesManaged.IV = pdb.GetBytes(16);
        }

        public AesCryptoKey(string key, string iv)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv))
            {
                throw new ArgumentException("Require key and iv string.");
            }

            AesManaged = CreateAesManaged();

            AesManaged.Key = Encoding.UTF8.GetBytes(key);
            AesManaged.IV = Encoding.UTF8.GetBytes(iv);
        }
        
        private static AesManaged CreateAesManaged()
        {
            var aesManaged = new AesManaged()
            {
                BlockSize = 128,
                KeySize = 256,
                Padding = PaddingMode.PKCS7,
                Mode = CipherMode.CBC,
            };

            return aesManaged;
        }
    }
}
