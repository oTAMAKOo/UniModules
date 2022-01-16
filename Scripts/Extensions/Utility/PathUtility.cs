﻿﻿﻿﻿﻿﻿﻿
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Extensions
{
    public static class PathUtility
    {
        public const char PathSeparator = '/';

        public enum FilePathType
        {
            NotFound,   // 不明.
            File,       // ファイル.
            Directory,  // フォルダ.
        }

        /// <summary> パス区切り文字を変換 </summary>
        public static string ConvertPathSeparator(string path)
        {
            return string.IsNullOrEmpty(path) ? null : path.Replace('\\', PathSeparator);
        }

        /// <summary> ファイルかフォルダかを判別 </summary>
        public static FilePathType GetFilePathType(string path)
        {
            if (File.Exists(path)) { return FilePathType.File; }

            if (Directory.Exists(path)) { return FilePathType.Directory; }

            return FilePathType.NotFound;
        }

        /// <summary> 2つの文字列を"/"で連結して、1つのパスに結合 </summary>
        public static string Combine(string path1, string path2)
        {
            return Combine(new string[] { path1, path2 });
        }

        /// <summary> 複数の文字列を"\"で連結して、1つのパスに結合 </summary>
        public static string Combine(string[] paths)
        {
            if (paths.IsEmpty()) { return string.Empty; }

            var builder = new StringBuilder();

            for (var i = 0; i < paths.Length; i++)
            {
                if (string.IsNullOrEmpty(paths[i])) { continue; }

                var path = ConvertPathSeparator(paths[i]);

                if (builder.Length != 0)
                {
                    // 先頭の"\"を削除.
                    path = path.TrimStart(PathSeparator);

                    // 末尾に\を追加.
                    builder.Append(PathSeparator);
                }

                // 末尾の"\"を削除.
                path = path.TrimEnd(PathSeparator);

                builder.Append(path);
            }

            return builder.ToString();
        }

        /// <summary> 絶対パスから相対パスに変換 </summary>
        public static string FullPathToRelativePath(string basePath, string targetPath)
        {
            var baseUri = new Uri(basePath);
            var targetUri = new Uri(targetPath);

            return baseUri.MakeRelativeUri(targetUri).ToString(); ;
        }

        /// <summary> 相対パスから絶対パスに変換 </summary>
        public static string RelativePathToFullPath(string basePath, string targetPath)
        {
            var origin = Environment.CurrentDirectory;

            Environment.CurrentDirectory = basePath;

            var path = Path.GetFullPath(targetPath);

            Environment.CurrentDirectory = origin;

            return path;
        }

        /// <summary> 指定されたパス文字列から拡張子を削除 </summary>
        public static string GetPathWithoutExtension(string path)
        {
            var extension = Path.GetExtension(path);

            if(string.IsNullOrEmpty(extension))
            {
                return path;
            }

            return path.Replace(extension, string.Empty);
        }
    }
}