
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Extensions;

namespace Extensions
{
    public class FileUtility
    {
        /// <summary>
        /// 指定されたファイルのハッシュ値を取得.
        /// </summary>
        public static string GetHash(string path)
        {
            byte[] bytes;

            // .NET FrameworkのMD5計算クラスを作成.
            var md5 = MD5.Create();

            // 対象ファイルを開い、ComputeHashメソッドを呼び出してMD5計算を行う
            using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                bytes = md5.ComputeHash(fs);
            }

            // 計算結果を16進数の文字列に変換.
            var md5str = new StringBuilder();

            foreach (var b in bytes)
            {
                md5str.Append(b.ToString("x2"));
            }

            return md5str.ToString();
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
