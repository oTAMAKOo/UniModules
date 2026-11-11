
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

            using (var memoryStream = new MemoryStream())
            {
                lock (aesCryptoKey)
                {
                    lock (aesCryptoKey.Encryptor)
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, aesCryptoKey.Encryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(value, 0, value.Length);

                            cryptoStream.FlushFinalBlock();
                        }
                    }
                }

                result = memoryStream.ToArray();
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
            
            using (var memoryStream = new MemoryStream())
            {
                lock (aesCryptoKey)
                {
                    lock (aesCryptoKey.Decryptor)
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, aesCryptoKey.Decryptor, CryptoStreamMode.Write))
                        {
                            cryptoStream.Write(value, 0, value.Length);

                            cryptoStream.FlushFinalBlock();
                        }
                    }
                }

                result = memoryStream.ToArray();
            }

            return result;
        }

        /// <summary>
        /// 文字列をAESで暗号化.
        /// </summary>
        public static string Encrypt(this string value, AesCryptoKey aesCryptoKey, bool escape = false)
        {
            if (string.IsNullOrEmpty(value)) { return null; }
            
            var bytes = Encoding.UTF8.GetBytes(value);

            bytes = Encrypt(bytes, aesCryptoKey);

            var result = Convert.ToBase64String(bytes);

            if (escape)
            {
                result = result.Replace('+', '-').Replace('/', '_');
            }

            return result;
        }

        /// <summary>
        /// 文字列をAESで復号化.
        /// </summary>
        public static string Decrypt(this string value, AesCryptoKey aesCryptoKey, bool escape = false)
        {
            if (string.IsNullOrEmpty(value)) { return null; }
            
            if (escape)
            {
                value = value.Replace('-', '+').Replace('_', '/');
            }

            var bytes = Convert.FromBase64String(value);
            
            bytes = Decrypt(bytes, aesCryptoKey);

            var result = Encoding.UTF8.GetString(bytes);

            // string.Lengthした際に終端にNull文字が混入する為Null文字を削る.
            return result.TrimEnd('\0');
        }
    }

    public class AesCryptoKey
    {
        //----- params -----
        
        protected static readonly byte[] Salt = { 0xe6, 0xdc, 0xff, 0x74, 0xad, 0xad, 0x7a, 0xee, 0xc5, 0xfe, 0x50, 0xaf, 0x4d, 0x08, 0x2d, 0x3c };

        //----- field -----

        protected AesManaged aesManaged = null;

        protected ICryptoTransform encryptor = null;
        protected ICryptoTransform decryptor = null;

        //----- property -----

        public byte[] Key { get { return aesManaged.Key; } }

        public byte[] Iv { get { return aesManaged.IV; } }

        public ICryptoTransform Encryptor
        {
            get
            {
                if (encryptor == null || !encryptor.CanReuseTransform)
                {
                    encryptor = CreateEncryptor();
                }

                return encryptor;
            }
        }

        public ICryptoTransform Decryptor
        {
            get
            {
                if (decryptor == null || !decryptor.CanReuseTransform)
                {
                    decryptor = CreateDecryptor();
                }

                return decryptor;
            }
        }

        public int BlockSize { get { return aesManaged.BlockSize; } }

        //----- method -----

        public AesCryptoKey(string password) : this(password, CreateDefaultAesManaged()) { }

        public AesCryptoKey(string password, AesManaged aesManaged)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Require password string.");
            }

            this.aesManaged = aesManaged;

            // 疑似乱数を使用してパスワードを暗号化.
            var pdb = new Rfc2898DeriveBytes(password, Salt, 64);

            aesManaged.Key = pdb.GetBytes(32);
            aesManaged.IV = pdb.GetBytes(16);
        }

        public AesCryptoKey(string key, string iv) : this(key, iv, CreateDefaultAesManaged()) { }

        public AesCryptoKey(string key, string iv, AesManaged aesManaged)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(iv))
            {
                throw new ArgumentException("Require key and iv string.");
            }

            this.aesManaged = aesManaged;

            aesManaged.Key = Encoding.UTF8.GetBytes(key);
            aesManaged.IV = Encoding.UTF8.GetBytes(iv);
        }

        public AesCryptoKey(byte[] key, byte[] iv) : this(key, iv, CreateDefaultAesManaged()) { }

        public AesCryptoKey(byte[] key, byte[] iv, AesManaged aesManaged)
        {
            if (key == null || iv == null)
            {
                throw new ArgumentException("Require key and iv string.");
            }

            this.aesManaged = aesManaged;

            aesManaged.Key = key;
            aesManaged.IV = iv;
        }

        private ICryptoTransform CreateEncryptor()
        {
            lock (aesManaged)
            {
                encryptor = aesManaged.CreateEncryptor();
            }
            
            return encryptor;
        }

        private ICryptoTransform CreateDecryptor()
        {
            lock (aesManaged)
            {
                decryptor = aesManaged.CreateDecryptor();
            }

            return decryptor;
        }
        
        private static AesManaged CreateDefaultAesManaged()
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
