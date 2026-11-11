
using System;
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
        public static string GetBytesReadable(long byteSize, string format = "0.00", bool truncate = false)
        {
            var readable = default(double);
            var suffix = string.Empty;
            var sign = byteSize < 0 ? "-" : "";

            byteSize = Math.Abs(byteSize);

            if (byteSize >= 0x1000000000000000) // EB
            {
                suffix = Suffix[SizeType.EB];
                readable = (double)byteSize / (1L << 60);
            }
            else if (byteSize >= 0x4000000000000) // PB
            {
                suffix = Suffix[SizeType.PB];
                readable = (double)byteSize / (1L << 50);
            }
            else if (byteSize >= 0x10000000000) // TB
            {
                suffix = Suffix[SizeType.TB];
                readable = (double)byteSize / (1L << 40);
            }
            else if (byteSize >= 0x40000000) // GB
            {
                suffix = Suffix[SizeType.GB];
                readable = (double)byteSize / (1L << 30);
            }
            else if (byteSize >= 0x100000) // MB
            {
                suffix = Suffix[SizeType.MB];
                readable = (double)byteSize / (1L << 20);
            }
            else if (byteSize >= 0x400) // KB
            {
                suffix = Suffix[SizeType.KB];
                readable = (double)byteSize / (1L << 10);
            }
            else
            {
                return byteSize.ToString("0") + Suffix[SizeType.B];
            }

            // 整数かどうか判定してフォーマットを分ける

            var text = string.Empty;

            if (truncate)
            {
                text = Math.Abs(readable % 1) < double.Epsilon
                       ? ((long)readable).ToString("0") // 小数点以下なし
                       : readable.ToString(format);     // 小数点あり
            }
            else
            {
                text = readable.ToString(format);
            }

            return sign + text + suffix;
        }
    }
}