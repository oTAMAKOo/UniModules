
using System.IO;
using Cysharp.Threading.Tasks;
using Extensions;
using MessagePack;
using MessagePack.Resolvers;
using Modules.MessagePack;

namespace Extensions
{
    public static class MessagePackFileUtility
    {
        public static async UniTask Write<T>(string filePath, T target, AesCryptoKey cryptoKey = null) where T : class
        {
            var directory = Path.GetDirectoryName(filePath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                var options = StandardResolverAllowPrivate.Options
                    .WithCompression(MessagePackCompression.Lz4BlockArray)
                    .WithResolver(UnityCustomResolver.Instance);

                var bytes = MessagePackSerializer.Serialize(target, options);

                if (cryptoKey != null)
                {
                    bytes = bytes.Encrypt(cryptoKey);
                }

                await fileStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        public static async UniTask<T> Read<T>(string filePath, AesCryptoKey cryptoKey = null) where T : class
        {
            #if UNITY_ANDROID

            // ※ AndroidではstreamingAssetsPathがWebRequestからしかアクセスできないのでtemporaryCachePathにファイルを複製する.

            if (filePath.StartsWith(UnityPathUtility.StreamingAssetsPath))
            {
                #if UNITY_ANDROID && !UNITY_EDITOR

                await AndroidUtility.CopyStreamingToTemporary(filePath);

                filePath = filePath.Replace(UnityPathUtility.StreamingAssetsPath, UnityPathUtility.TemporaryCachePath);

                #endif
            }

            #endif

            if (!File.Exists(filePath)){ return null; }

            T target = null;

            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var bytes = new byte[fileStream.Length];

                await fileStream.ReadAsync(bytes, 0, bytes.Length);

                if (cryptoKey != null)
                {
                    bytes = bytes.Decrypt(cryptoKey);
                }

                var options = StandardResolverAllowPrivate.Options
                    .WithCompression(MessagePackCompression.Lz4BlockArray)
                    .WithResolver(UnityCustomResolver.Instance);

                target = MessagePackSerializer.Deserialize<T>(bytes, options);
            }

            return target;
        }
    }
}