
namespace Extensions
{
    public static class ByteDataUtility
    {
        /// <summary> バイト数を100MBのような表示用文字列に変換 </summary>
        public static string GetBytesReadable(long byteSize, string format = "0.###")
        {
            var readable = default(long);
            var suffix = string.Empty;
            var sign = byteSize < 0 ? "-" : "";

            byteSize = byteSize < 0 ? -byteSize : byteSize;

            if (byteSize >= 0x1000000000000000)
            {
                suffix = "EB";
                readable = (byteSize >> 50);
            }
            else if (byteSize >= 0x4000000000000)
            {
                suffix = "PB";
                readable = (byteSize >> 40);
            }
            else if (byteSize >= 0x10000000000)
            {
                suffix = "TB";
                readable = (byteSize >> 30);
            }
            else if (byteSize >= 0x40000000)
            {
                suffix = "GB";
                readable = (byteSize >> 20);
            }
            else if (byteSize >= 0x100000)
            {
                suffix = "MB";
                readable = (byteSize >> 10);
            }
            else if (byteSize >= 0x400)
            {
                suffix = "KB";
                readable = byteSize;
            }
            else
            {
                return byteSize.ToString("0 B");
            }

            readable /= 1024;

            return sign + readable.ToString(format) + suffix;
        }
    }
}