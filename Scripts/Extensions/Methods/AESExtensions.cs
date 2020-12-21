
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Extensions
{
    public static class AESExtension
    {
        //----- params -----

        private const string DefaultPassword = "SSAHb5DqFV241491";

        private static readonly byte[] Salt = { 0xe6, 0xdc, 0xff, 0x74, 0xad, 0xad, 0x7a, 0xee, 0xc5, 0xfe, 0x50, 0xaf, 0x4d, 0x08, 0x2d, 0x3c };

        //----- field -----

        private static AesManaged aesManaged = null;

        //----- property -----

        public static AesManaged AesManaged
        {
            get { return aesManaged ?? (aesManaged = CreateAesManaged(DefaultPassword)); }
        }

        //----- method -----

        private static AesManaged CreateDefaultAesManaged()
        {
            var aesManaged = new AesManaged();

            aesManaged.BlockSize = 128;
            aesManaged.Padding = PaddingMode.PKCS7;
            aesManaged.Mode = CipherMode.CBC;

            return aesManaged;
        }

        /// <summary>
        /// パスワードを指定してAesManagedを生成.
        /// </summary>
        public static AesManaged CreateAesManaged(string password)
        {
            if (string.IsNullOrEmpty(password)) { return null; }

            // 疑似乱数を使用してパスワードを暗号化.
            var pdb = new Rfc2898DeriveBytes(password, Salt, 64);

            var aesManaged = CreateDefaultAesManaged();

            aesManaged.Key = pdb.GetBytes(32);
            aesManaged.IV = pdb.GetBytes(16);

            return aesManaged;
        }

        /// <summary>
        /// キー、ベクトルを指定してAesManagedを生成.
        /// </summary>
        public static AesManaged CreateAesManaged(string key, string iv)
        {
            if (string.IsNullOrEmpty(key)) { return null; }

            if (string.IsNullOrEmpty(iv)) { return null; }

            var aesManaged = CreateDefaultAesManaged();

            aesManaged.Key = Encoding.UTF8.GetBytes(key);
            aesManaged.IV = Encoding.UTF8.GetBytes(iv);

            return aesManaged;
        }

        /// <summary>
        /// バイト配列をAESで暗号化.
        /// </summary>
        public static byte[] Encrypt(this byte[] value, AesManaged aesManaged = null)
        {
            if (value == null || value.IsEmpty()) { return null; }

            if (aesManaged == null)
            {
                aesManaged = AesManaged;
            }

            byte[] result = null;

            using (var encryptor = aesManaged.CreateEncryptor())
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
        public static byte[] Decrypt(this byte[] value, AesManaged aesManaged = null)
        {
            if (value == null || value.IsEmpty()) { return null; }

            if (aesManaged == null)
            {
                aesManaged = AesManaged;
            }

            byte[] result = null;

            using (var decryptor = aesManaged.CreateDecryptor())
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
        public static string Encrypt(this string value, AesManaged aesManaged)
        {
            if (string.IsNullOrEmpty(value)) { return null; }

            string result = null;

            using (var encryptor = aesManaged.CreateEncryptor())
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
        public static string Decrypt(this string value, AesManaged aesManaged)
        {
            if (string.IsNullOrEmpty(value)) { return null; }

            string result = null;

            using (var decryptor = aesManaged.CreateDecryptor())
            {
                var encrypted = Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/'));
                var fromEncrypt = new byte[encrypted.Length];

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
}
