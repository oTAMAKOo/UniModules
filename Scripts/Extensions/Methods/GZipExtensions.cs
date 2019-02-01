
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace Extensions
{
    public static class GZipExtensions
    {
        /// <summary> GZip形式で圧縮 </summary>
        public static byte[] Compress(this byte[] bytes)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, false))
                {
                    gzipStream.Write(bytes, 0, bytes.Length);
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary> GZip形式で圧縮 </summary>
        public static byte[] Compress(this string str, Encoding encoding)
        {
            var bytes = encoding.GetBytes(str);

            return Compress(bytes);
        }

        /// <summary> GZip形式で圧縮 </summary>
        public static byte[] Compress<T>(this T target) where T : class
        {
            var binaryFormatter = new BinaryFormatter();

            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, target);

                return Compress(memoryStream.ToArray());
            }
        }

        /// <summary> GZip形式で圧縮されたデータを解凍 </summary>
        public static byte[] Decompress(this byte[] bytes)
        {
            if (bytes == null) { return null; }

            if (bytes.IsEmpty()) { return new byte[0]; }

            var buffer = new byte[1024];

            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
                {
                    while (true)
                    {
                        var readSize = gzipStream.Read(buffer, 0, buffer.Length);

                        if (readSize == 0)
                        {
                            break;
                        }

                        memoryStream.Write(buffer, 0, readSize);
                    }
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary> GZip形式で圧縮されたデータを解凍 </summary>
        public static string Decompress(this byte[] bytes, Encoding encoding)
        {
            var decompressedBytes = Decompress(bytes);

            if (decompressedBytes == null || decompressedBytes.IsEmpty())
            {
                return string.Empty;
            }

            return encoding.GetString(decompressedBytes);
        }

        /// <summary> GZip形式で圧縮されたデータを解凍 </summary>
        public static T Decompress<T>(this byte[] bytes) where T : class
        {
            var binaryFormatter = new BinaryFormatter();

            var decompressedBytes = Decompress(bytes);

            if (decompressedBytes == null || decompressedBytes.IsEmpty())
            {
                return default(T);
            }

            using (var memoryStream = new MemoryStream(decompressedBytes))
            {
                binaryFormatter.Deserialize(memoryStream);

                return memoryStream as T;
            }
        }
    }
}
