
using System.Collections.Generic;

namespace Extensions
{
    public static class ByteDataUtility
    {
        public enum SizeType
        {
            /// <summary> バイト </summary>
            B,

            /// <summary> キロバイト (1000 B) </summary>
            KB,

            /// <summary> メガバイト (1000 kB) </summary>
            MB,

            /// <summary> ギガバイト (1000 MB) </summary>
            GB,

            /// <summary> テラバイト (1000 GB) </summary>
            TB,

            /// <summary> ペタバイト (1000 TB) </summary>
            PB,

            /// <summary> エクサバイト (1000 PB) </summary>
            EB,
        }

        public static readonly Dictionary<SizeType, string> DefaultSuffix = new Dictionary<SizeType, string>()
        {
            { SizeType.B, "B" },
            { SizeType.KB, "KB" },
            { SizeType.MB, "MB" },
            { SizeType.GB, "GB" },
            { SizeType.TB, "TB" },
            { SizeType.PB, "PB" },
            { SizeType.EB, "EB" },
        };

        public static Dictionary<SizeType, string> Suffix = new Dictionary<SizeType, string>(DefaultSuffix);

        /// <summary> バイト数を100MBのような表示用文字列に変換 </summary>
        public static string GetBytesReadable(long byteSize, string format = "0.###")
        {
            var readable = default(long);
            var suffix = string.Empty;
            var sign = byteSize < 0 ? "-" : "";

            byteSize = byteSize < 0 ? -byteSize : byteSize;

            if (byteSize >= 0x1000000000000000)
            {
                suffix = Suffix[SizeType.EB];
                readable = (byteSize >> 50);
            }
            else if (byteSize >= 0x4000000000000)
            {
                suffix = Suffix[SizeType.PB];
                readable = (byteSize >> 40);
            }
            else if (byteSize >= 0x10000000000)
            {
                suffix = Suffix[SizeType.TB];
                readable = (byteSize >> 30);
            }
            else if (byteSize >= 0x40000000)
            {
                suffix = Suffix[SizeType.GB];
                readable = (byteSize >> 20);
            }
            else if (byteSize >= 0x100000)
            {
                suffix = Suffix[SizeType.MB];
                readable = (byteSize >> 10);
            }
            else if (byteSize >= 0x400)
            {
                suffix = Suffix[SizeType.KB];
                readable = byteSize;
            }
            else
            {
                return byteSize.ToString("0") + Suffix[SizeType.B];
            }

            readable /= 1024;

            return sign + readable.ToString(format) + suffix;
        }
    }
}