
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Extensions
{
    public sealed class FileUtility
    {
        /// <summary>
        /// 指定されたファイルのSHA256ハッシュ値を取得.
        /// </summary>
        public static string GetHash(string path)
        {
            byte[] bytes;
            
            var crypt256 = new SHA256CryptoServiceProvider();

            using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bytes = crypt256.ComputeHash(fs);

                crypt256.Clear();
            }
            
            var hashedText = new StringBuilder();

            foreach (var b in bytes)
            {
                hashedText.Append(b.ToString("x2"));
            }

            return hashedText.ToString();
        }

        /// <summary>
        /// 指定されたファイルのCRC32ハッシュ値を取得.
        /// </summary>
        public static string GetCRC(string path)
        {
            byte[] bytes;

            var crc32 = new CRC32();

            using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bytes = crc32.ComputeHash(fs);

                crc32.Clear();
            }

            var hashedText = new StringBuilder();

            foreach (var b in bytes)
            {
                hashedText.Append(b.ToString("x2"));
            }

            return hashedText.ToString();
        }

        /// <summary>
        /// 指定されたファイルがロックされているかどうか.
        /// </summary>
        /// <param name="path">検証したいファイルへのフルパス</param>
        /// <returns>ロックされているかどうか</returns>
        public static bool IsFileLocked(string path)
        {
            FileStream stream = null;

            try
            {
                stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch
            {
                return true;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            return false;
        }

		/// <summary> バイト数を100MBのような表示用文字列に変換 </summary>
		public static string GetBytesReadable(long byteSize)
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

			return sign + readable.ToString("0.### ") + suffix;
		}
	}
}
