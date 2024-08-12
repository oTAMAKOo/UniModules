
using System.IO;
using Cysharp.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;
using Modules.MessagePack;

namespace Extensions
{
    public static class MessagePackFileUtility
    {
        public static void Write<T>(string filePath, T target, AesCryptoKey cryptoKey = null) where T : class
        {
            CreateDirectory(filePath);

            var bytes = Serialize(target, cryptoKey);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fileStream.Write(bytes, 0, bytes.Length);
            }
        }

        public static async UniTask WriteAsync<T>(string filePath, T target, AesCryptoKey cryptoKey = null) where T : class
        {
            CreateDirectory(filePath);

            var bytes = Serialize(target, cryptoKey);

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                await fileStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        public static T Read<T>(string filePath, AesCryptoKey cryptoKey = null) where T : class
        {
            if (!File.Exists(filePath)){ return null; }

            byte[] bytes = null;

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bytes = new byte[fileStream.Length];

                var _ = fileStream.Read(bytes, 0, bytes.Length);
            }

            var target = Deserialize<T>(bytes, cryptoKey);

            return target;
        }

        public static async UniTask<T> ReadAsync<T>(string filePath, AesCryptoKey cryptoKey = null) where T : class
        {
            filePath = await CopyStreamingToTemporary(filePath);

            if (!File.Exists(filePath)){ return null; }

            byte[] bytes = null;

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bytes = new byte[fileStream.Length];

                var _ = await fileStream.ReadAsync(bytes, 0, bytes.Length);
            }

            var target = Deserialize<T>(bytes, cryptoKey);

            return target;
        }

        private static string CreateDirectory(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return directory;
        }

        #pragma warning disable CS1998

        private static async UniTask<string> CopyStreamingToTemporary(string filePath)
        {
            #if UNITY_ANDROID

            // ※ AndroidではstreamingAssetsPathがWebRequestからしかアクセスできないのでtemporaryCachePathにファイルを複製する.

            if (filePath.StartsWith(UnityPathUtility.StreamingAssetsPath))
            {
                #if UNITY_ANDROID && !UNITY_EDITOR

                await AndroidUtility.CopyStreamingToTemporary(filePath);

                filePath = AndroidUtility.ConvertStreamingAssetsLoadPath(filePath);

                #endif
            }

            #endif

            return filePath;
        }

        #pragma warning restore CS1998

        private static byte[] Serialize<T>(T target, AesCryptoKey cryptoKey = null) where T : class
        {
            var options = StandardResolverAllowPrivate.Options
            .WithCompression(MessagePackCompression.Lz4BlockArray)
            .WithResolver(UnityCustomResolver.Instance);

            var bytes = MessagePackSerializer.Serialize(target, options);

            if (cryptoKey != null)
            {
                bytes = bytes.Encrypt(cryptoKey);
            }

            return bytes;
        }

        private static T Deserialize<T>(byte[] bytes, AesCryptoKey cryptoKey = null) where T : class
        {
            if (bytes.IsEmpty()){ return null; }

            if (cryptoKey != null)
            {
                bytes = bytes.Decrypt(cryptoKey);
            }

            var options = StandardResolverAllowPrivate.Options
            .WithCompression(MessagePackCompression.Lz4BlockArray)
            .WithResolver(UnityCustomResolver.Instance);

            var target = MessagePackSerializer.Deserialize<T>(bytes, options);

            return target;
        }
    }
}