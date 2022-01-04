
using System;
using System.IO;
using System.Text;
using Extensions;

namespace Modules.Crypto
{
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

    public interface IKeyFileManager
    {
        void Create(string filePath, string key, string iv);

        KeyData Load(string filePath);

        KeyData Load(byte[] bytes);
    }

    public abstract class KeyFileManager<TInstance> : Singleton<TInstance>, IKeyFileManager where TInstance : KeyFileManager<TInstance>
    {
        //----- params -----

        private const string Separator = ":*";

        //----- field -----

        //----- property -----

        //----- method -----

        public void Create(string filePath, string key, string iv)
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

            bytes = CustomEncode(bytes);

            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                file.Write(bytes, 0, bytes.Length);
            }
        }

        public KeyData Load(string filePath)
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

        public KeyData Load(byte[] bytes)
        {
            if (bytes == null || bytes.IsEmpty()){ return null; }

            bytes = CustomDecode(bytes);

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

        protected virtual byte[] CustomEncode(byte[] bytes) { return bytes; }

        protected virtual byte[] CustomDecode(byte[] bytes) { return bytes; }
    }
}
