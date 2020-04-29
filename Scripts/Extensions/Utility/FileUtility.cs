
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
            
            var crypto256 = new SHA256CryptoServiceProvider();

            using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bytes = crypto256.ComputeHash(fs);

                crypto256.Clear();
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
    }
}
