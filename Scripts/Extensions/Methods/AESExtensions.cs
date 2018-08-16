﻿
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Extensions
{
    public static class AESExtension
    {
        private static readonly byte[] Salt = { 0xe6, 0xdc, 0xff, 0x74, 0xad, 0xad, 0x7a, 0xee, 0xc5, 0xfe, 0x50, 0xaf, 0x4d, 0x08, 0x2d, 0x3c };

        public static RijndaelManaged CreateRijndael(string password, bool passwordEncryption = true)
        {
            var rijndael = new RijndaelManaged();

            rijndael.BlockSize = 128;
            rijndael.Padding = PaddingMode.PKCS7;
            rijndael.Mode = CipherMode.ECB;

            if (passwordEncryption)
            {
                // 疑似乱数を使用してパスワードを暗号化.
                var pdb = new Rfc2898DeriveBytes(password, Salt, 64);

                rijndael.Key = pdb.GetBytes(32);
                rijndael.IV = pdb.GetBytes(16);
            }
            else
            {
                rijndael.Key = Encoding.UTF8.GetBytes(password);
            }

            return rijndael;
        }

        /// <summary>
        /// バイト配列をAESで暗号化.
        /// </summary>
        public static byte[] Encrypt(this byte[] value, RijndaelManaged rijndael)
        {
            if (value == null || value.IsEmpty()) { return null; }

            byte[] result = null;

            using (var encryptor = rijndael.CreateEncryptor())
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
        public static byte[] Decrypt(this byte[] value, RijndaelManaged rijndael)
        {
            if (value == null || value.IsEmpty()) { return null; }

            byte[] result = null;

            using (var decryptor = rijndael.CreateDecryptor())
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
        public static string Encrypt(this string value, RijndaelManaged rijndael)
        {
            if (string.IsNullOrEmpty(value)) { return null; }

            string result = null;

            using (var encryptor = rijndael.CreateEncryptor())
            {
                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                    {
                        var toEncrypt = Encoding.UTF8.GetBytes(value);

                        cryptoStream.Write(toEncrypt, 0, toEncrypt.Length);
                        cryptoStream.FlushFinalBlock();

                        var encrypted = memoryStream.ToArray();

                        result = ToHexString(encrypted);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// 文字列をAESで復号化.
        /// </summary>
        public static string Decrypt(this string value, RijndaelManaged rijndael)
        {
            if (string.IsNullOrEmpty(value)) { return null; }

            string result = null;

            using (var decryptor = rijndael.CreateDecryptor())
            {
                var encrypted = FromHexString(value);
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

        // バイト配列を16進文字列に変換.
        private static string ToHexString(byte[] value)
        {
            var ret = string.Empty;

            foreach (var b in value)
            {
                if (b < 16)
                {
                    ret += "0";
                }
                ret += Convert.ToString(b, 16);
            }

            return ret;
        }

        // 16進文字列をバイト配列に変換.
        private static byte[] FromHexString(string value)
        {
            var length = value.Length / 2;
            var bytes = new byte[length];

            var j = 0;

            for (var i = 0; i < length; ++i)
            {
                bytes[i] = Convert.ToByte(value.Substring(j, 2), 16);
                j += 2;
            }

            return bytes;
        }
    }
}