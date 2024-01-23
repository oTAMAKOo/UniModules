
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;

namespace Extensions
{
    public static class CompressionExtensions
    {
        public enum CompressionAlgorithm
        {
            GZip,
            Deflate,
        }

        //-------------------------------------------
        // Compress
        //-------------------------------------------

        /// <summary> 圧縮 </summary>
        public static byte[] Compress(this byte[] bytes, CompressionAlgorithm algorithm = CompressionAlgorithm.GZip)
        {
            switch (algorithm)
            {
                case CompressionAlgorithm.GZip:
                    bytes = CompressGZip(bytes);
                    break;
                case CompressionAlgorithm.Deflate:
                    bytes = CompressDeflate(bytes);
                    break;
            }

            return bytes;
        }

        private static byte[] CompressGZip(byte[] bytes)
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

        private static byte[] CompressDeflate(byte[] bytes)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress, false))
                {
                    deflateStream.Write(bytes, 0, bytes.Length);
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary> 圧縮 </summary>
        public static byte[] Compress(this string str, Encoding encoding, CompressionAlgorithm algorithm = CompressionAlgorithm.GZip)
        {
            var bytes = encoding.GetBytes(str);

            return Compress(bytes, algorithm);
        }

        /// <summary> 圧縮 </summary>
        public static byte[] Compress<T>(this T target, CompressionAlgorithm algorithm = CompressionAlgorithm.GZip) where T : class
        {
            var binaryFormatter = new BinaryFormatter();

            using (var memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, target);

                return Compress(memoryStream.ToArray(), algorithm);
            }
        }

        //-------------------------------------------
        // Decompress
        //-------------------------------------------

        /// <summary> 圧縮されたデータを解凍 </summary>
        public static byte[] Decompress(this byte[] bytes, CompressionAlgorithm algorithm = CompressionAlgorithm.GZip)
        {
            if (bytes == null) { return null; }

            if (bytes.IsEmpty()) { return new byte[0]; }

            switch (algorithm)
            {
                case CompressionAlgorithm.GZip:
                    bytes = DecompressGZip(bytes);
                    break;
                case CompressionAlgorithm.Deflate:
                    bytes = DecompressDeflate(bytes);
                    break;
            }

            return bytes;
        }

        private static byte[] DecompressGZip(byte[] bytes)
        {
            var buffer = new byte[1024];

            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
                {
                    while (true)
                    {
                        var readSize = gzipStream.Read(buffer, 0, buffer.Length);

                        if (readSize == 0) { break; }

                        memoryStream.Write(buffer, 0, readSize);
                    }
                }

                return memoryStream.ToArray();
            }
        }

        private static byte[] DecompressDeflate(byte[] bytes)
        {
            var buffer = new byte[1024];

            using (var memoryStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(new MemoryStream(bytes), CompressionMode.Decompress))
                {
                    while (true)
                    {
                        var readSize = deflateStream.Read(buffer, 0, buffer.Length);

                        if (readSize == 0) { break; }

                        memoryStream.Write(buffer, 0, readSize);
                    }
                }

                return memoryStream.ToArray();
            }
        }

        /// <summary> 圧縮されたデータを解凍 </summary>
        public static string Decompress(this byte[] bytes, Encoding encoding, CompressionAlgorithm algorithm = CompressionAlgorithm.GZip)
        {
            var decompressedBytes = Decompress(bytes, algorithm);

            if (decompressedBytes == null || decompressedBytes.IsEmpty())
            {
                return string.Empty;
            }

            return encoding.GetString(decompressedBytes);
        }

        /// <summary> 圧縮されたデータを解凍 </summary>
        public static T Decompress<T>(this byte[] bytes, CompressionAlgorithm algorithm = CompressionAlgorithm.GZip) where T : class
        {
            var binaryFormatter = new BinaryFormatter();

            var decompressedBytes = Decompress(bytes, algorithm);

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
