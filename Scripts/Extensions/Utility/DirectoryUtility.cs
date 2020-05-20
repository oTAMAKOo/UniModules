
using System;
using System.IO;
using System.Collections.Generic;

namespace Extensions
{
    public static class DirectoryUtility
    {
        /// <summary>
        /// 指定されたフォルダ以下の空フォルダを削除.
        /// ※ 渡すパス次第で危険な挙動になるので実行する前に渡すパスを確認する事.
        /// </summary>
        /// <param name="targetDir"></param>
        public static string[] DeleteEmpty(string targetDir)
        {
            var list = new List<string>();

            if (!Directory.Exists(targetDir)) { return list.ToArray(); }

            foreach (var subdir in Directory.GetDirectories(targetDir))
            {
                list.AddRange(DeleteEmptyInternal(subdir));
            }

            return list.ToArray();
        }

        /// <summary>
        /// 空フォルダを削除(指定されたディレクトリを含む).
        /// </summary>
        /// <param name="targetDir"></param>
        private static string[] DeleteEmptyInternal(string targetDir)
        {
            var list = new List<string>();

            if (!Directory.Exists(targetDir)) { return list.ToArray(); }

            foreach (var subdir in Directory.GetDirectories(targetDir))
            {
                list.AddRange(DeleteEmptyInternal(subdir));
            }

            if (Directory.GetFileSystemEntries(targetDir).IsEmpty())
            {
                // Unityのフォルダには.metaが対になって生成されるので消す.
                var metaFile = targetDir + ".meta";

                if (File.Exists(metaFile))
                {
                    File.Delete(metaFile);
                }

                Directory.Delete(targetDir);

                list.Add(targetDir);
            }

            return list.ToArray();
        }

        /// <summary>
        /// ディレクトリコピー.
        /// </summary>
        public static string[] Clone(string sourcePath, string copyPath, Func<string, bool> check = null, bool attributes = false, bool overwrite = true)
        {
            var result = new List<string>();

            if (!Directory.Exists(copyPath))
            {
                Directory.CreateDirectory(copyPath);

                // 属性もコピー.
                if (attributes)
                {
                    File.SetAttributes(copyPath, File.GetAttributes(sourcePath));
                }

                result.Add(copyPath);
            }

            var files = Directory.GetFiles(sourcePath);

            // ファイルをコピー.
            foreach (var file in files)
            {
                if (check == null || check(file))
                {
                    var newFile = Path.Combine(copyPath, Path.GetFileName(file));

                    if (File.Exists(file))
                    {
                        if (overwrite)
                        {
                            File.Copy(file, newFile, true);
                            result.Add(newFile);
                        }
                    }
                    else
                    {
                        File.Copy(file, newFile);
                        result.Add(newFile);
                    }
                }
            }

            // 再帰的にコピー.
            foreach (var dir in Directory.GetDirectories(sourcePath))
            {
                var cloned = Clone(dir, Path.Combine(copyPath, Path.GetFileName(dir)), check, attributes, overwrite);

                result.AddRange(cloned);
            }

            return result.ToArray();
        }

        /// <summary>
        /// 指定されたディレクトリ内のファイル / フォルダを削除.
        /// </summary>
        public static void Clean(string path)
        {
            if (string.IsNullOrEmpty(path)) { return; }

            if (!Directory.Exists(path)) { return; }

            // ファイル削除.

            var directoryInfo = new DirectoryInfo(path);

            foreach (var file in directoryInfo.GetFiles())
            {
                if (FileUtility.IsFileLocked(file.FullName)) { continue; }

                try
                {
                    file.Delete();
                }
                catch (Exception)
                {
                    // ignored.
                }
            }

            // フォルダ削除.

            var directories = Directory.GetDirectories(path);

            foreach (var directory in directories)
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
