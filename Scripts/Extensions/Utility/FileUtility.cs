
using System;
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

		/// <summary> 指定されたファイルがロックされているかどうか. </summary>
		/// <param name="filePath">検証したいファイルへのフルパス</param>
		/// <returns>ロックされているかどうか</returns>
		public static bool IsFileLocked(string filePath)
		{
			FileStream stream = null;

			try
			{
				stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
			}
			catch (DirectoryNotFoundException)
			{
				return false;
			}
			catch (FileNotFoundException)
			{
				return false;
			}
			catch (IOException)
			{
				if (File.Exists(filePath))
				{
					return true;
				}
			}
			catch (Exception)
			{
				return false;
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
