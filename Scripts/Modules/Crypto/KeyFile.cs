
using System;
using System.IO;
using System.Text;
using Extensions;

namespace Modules.Crypto
{
    public static class KeyFile
    {
        //----- params -----

        public sealed class KeyData
        {
            public string Key { get; private set; }

            public string Iv { get; private set; }

            public KeyData(string key, string iv)
            {
                Key = key;
                Iv = iv;
            }
        }

        private const string Separator = ":*";

        //----- field -----

        //----- property -----

        //----- method -----

        public static void Create(string filePath, string key, string iv)
        {
            var directory = Path.GetDirectoryName(filePath);

            if (Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var guid = Guid.NewGuid().ToString();

            var aesKey = new AesCryptoKey(guid);

            var encryptKey = key.Encrypt(aesKey);

            var encryptIv = iv.Encrypt(aesKey);

            var str = encryptKey + Separator + encryptIv + Separator + guid;

            var bytes = Encoding.UTF8.GetBytes(str);

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)~bytes[i];
            }

            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                file.Write(bytes, 0, bytes.Length);
            }
        }

        public static KeyData Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException();
            }

            var bytes = new byte[0];

            using (var fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var memoryStream = new MemoryStream())
                {
                    fileStream.CopyTo(memoryStream);

                    bytes = memoryStream.ToArray();
                }
            }

            return Load(bytes);
        }

        public static KeyData Load(byte[] bytes)
        {
            if (bytes == null || bytes.IsEmpty()){ return null; }

            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)~bytes[i];
            }

            var str = Encoding.UTF8.GetString(bytes);

            var parts = str.Split(new string[]{ Separator }, StringSplitOptions.None);

            var encryptKey = parts.ElementAtOrDefault(0, string.Empty);
            var encryptIv = parts.ElementAtOrDefault(1, string.Empty);
            var fileKey = parts.ElementAtOrDefault(2, string.Empty);

            if (string.IsNullOrEmpty(encryptKey) || string.IsNullOrEmpty(encryptIv) || string.IsNullOrEmpty(fileKey))
            {
                return null;
            }

            var aesKey = new AesCryptoKey(fileKey);

            var key = encryptKey.Decrypt(aesKey);
            var iv = encryptIv.Decrypt(aesKey);

            return new KeyData(key, iv);
        }
    }
}
