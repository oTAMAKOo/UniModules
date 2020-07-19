
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Extensions;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Extensions
{
    public static class UnityPathUtility
    {
        //----- params -----
        
        public const string AssetsFolder = "Assets";

        public const string ResourcesFolder = "Resources";

        public const string LibraryFolder = "Library";

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary> プロジェクト名取得 </summary>
        public static string GetProjectName()
        {
            var s = Application.dataPath.Split(PathUtility.PathSeparator);
            var projectName = s[s.Length - 2];

            return projectName;
        }

        /// <summary> Assetsフォルダまでのフルパスを取得 </summary>
        public static string GetProjectFolderPath()
        {
            return Application.dataPath.Replace(AssetsFolder, string.Empty);
        }

        /// <summary> ストリーミング用のアセットパスを取得 </summary>
        public static string GetStreamingAssetsPath()
        {
            var streamingAssetsPath = string.Empty;

            if (Application.isEditor)
            {
                // Use the build output folder directly.
                streamingAssetsPath = "file://" + Environment.CurrentDirectory.Replace("\\", PathUtility.PathSeparator.ToString());
            }
            else if (Application.isMobilePlatform)
            {
                #if UNITY_IOS

                streamingAssetsPath = Application.streamingAssetsPath + "/Raw/"; 
                
                #elif UNITY_ANDROID
                
                streamingAssetsPath = "jar:file://" + Application.streamingAssetsPath + "!assets/";
                
                #endif
            }
            else if(Application.isConsolePlatform)
            {
                streamingAssetsPath = "file://" + Application.streamingAssetsPath;
            }

            return streamingAssetsPath;
        }

        /// <summary> Assets/xxx/sourceDir/yyy/zzz.asset → yyy/zzz.assetに変換 </summary>
        public static string GetLocalPath(string path, string rootDir)
        {
            if (path.StartsWith(rootDir))
            {
                path = path.Substring(rootDir.Length);

                var separator = new string(new char[] { PathUtility.PathSeparator });

                if (path.StartsWith(separator))
                {
                    path = path.Substring(separator.Length);
                }
            }

            return path;
        }

        /// <summary> Assetsからの相対パスから絶対パスに変換  </summary>
        public static string RelativePathToFullPath(string path)
        {
            var u1 = new Uri(Application.dataPath);
            var u2 = new Uri(u1, path);

            return PathUtility.ConvertPathSeparator(u2.LocalPath);
        }

        /// <summary> Assetsからの相対パスを生成 </summary>
        public static string MakeRelativePath(string path)
        {
            var assetFolderUri = new Uri(Application.dataPath);
            var targetUri = new Uri(path);

            return assetFolderUri.MakeRelativeUri(targetUri).ToString();
        }

        /// <summary> "Assets"を含まないAssetのパスに変換 </summary>
        public static string ConvertProjectPath(string assetPath)
        {
            if (assetPath.StartsWith(AssetsFolder + PathUtility.PathSeparator))
            {
                return assetPath.Substring((AssetsFolder + PathUtility.PathSeparator).Length);
            }

            if (assetPath.StartsWith(Application.dataPath))
            {
                return assetPath.Substring(Application.dataPath.Length);
            }

            return null;
        }

        /// <summary> パスをUnityが取り扱うパスに変換 </summary>
        public static string ConvertFullPathToAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            path = PathUtility.ConvertPathSeparator(path);

            if (path.Contains(Application.dataPath))
            {
                if (path.LastOrDefault() == PathUtility.PathSeparator)
                {
                    path = path.Remove(path.Length - 1);
                }

                return PathUtility.Combine(AssetsFolder, path.Replace(Application.dataPath, string.Empty));
            }

            return null;
        }

        /// <summary> Unityが取り扱うパスをフルパスに変換  </summary>
        public static string ConvertAssetPathToFullPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }

            if (!assetPath.StartsWith(AssetsFolder))
            {
                throw new ArgumentException(@"This path not start Assets.", assetPath);
            }

            assetPath = PathUtility.ConvertPathSeparator(assetPath);

            return PathUtility.Combine(GetProjectFolderPath(), assetPath);
        }

        /// <summary> 指定パスをResources.Loadで読み込みできるパスへ変換  </summary>
        public static string ConvertResourcesLoadPath(string assetPath)
        {
            if (!string.IsNullOrEmpty(assetPath))
            {
                var resourcesFolder = ResourcesFolder + PathUtility.PathSeparator;

                // Resourcesフォルダまでのパスを削除.
                var index = assetPath.IndexOf(resourcesFolder, StringComparison.Ordinal);

                if (0 <= index)
                {
                    assetPath = assetPath.Substring(index + resourcesFolder.Length);
                }
            }

            var folder = Path.GetDirectoryName(assetPath);
            var name = Path.GetFileNameWithoutExtension(assetPath);
            var path = PathUtility.Combine(folder, name);

            return path;
        }

        /// <summary> フォルダを再帰的に処理して含まれるファイルのパスをmetaファイルを除いて返します </summary>
        public static string[] ExpnadFiles(string path)
        {
            var result = new List<string>();

            // 空文字は除外.
            if (string.IsNullOrEmpty(path))
            {
                return new string[0];
            }

            // ディレクトリでない場合は、単一ファイルなので自身を返す.
            if (!Directory.Exists(path))
            {
                result.Add(path);
            }

            // サブディレクトリーに対して再帰的に処理.
            foreach (var directory in Directory.GetDirectories(path))
            {
                foreach (var file in ExpnadFiles(directory))
                {
                    result.Add(file);
                }
            }

            // 対象フォルダー内で.meta以外のファイルについて処理.
            foreach (var file in Directory.GetFiles(path).Where(x => !x.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)))
            {
                result.Add(file);
            }

            return result.ToArray();
        }
    }
}
