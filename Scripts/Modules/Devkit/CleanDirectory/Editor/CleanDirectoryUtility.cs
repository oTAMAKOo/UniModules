﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.Prefs;
using UnityEditor;

namespace Modules.Devkit.CleanDirectory
{
	public static class CleanDirectoryUtility
	{
        public static class Prefs
        {
            public static bool cleanOnSave
            {
                get
                {
                    return ProjectPrefs.GetBool("CleanDirectory-cleanOnSave", false);
                }
                set
                {
                    ProjectPrefs.SetBool("CleanDirectory-cleanOnSave", value);
                }
            }
        }

        // return: Is this directory empty?
        delegate bool IsEmptyDirectory(DirectoryInfo dirInfo, bool areSubDirsEmpty);

        public static void FillEmptyDirList(out List<DirectoryInfo> emptyDirs)
        {
            var newEmptyDirs = new List<DirectoryInfo>();

            emptyDirs = newEmptyDirs;

            var assetDir = new DirectoryInfo(Application.dataPath);

            WalkDirectoryTree(assetDir, (dirInfo, areSubDirsEmpty) =>
            {
                bool isDirEmpty = areSubDirsEmpty && DirHasNoFile(dirInfo);
                if (isDirEmpty)
                    newEmptyDirs.Add(dirInfo);
                return isDirEmpty;
            });
        }

        public static string GetRelativePath(string filespec, string folder)
        {
            Uri pathUri = new Uri(filespec);

            // Folders must end in a slash
            if (!folder.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                folder += Path.DirectorySeparatorChar;
            }
            Uri folderUri = new Uri(folder);
            return Uri.UnescapeDataString(folderUri.MakeRelativeUri(pathUri).ToString().Replace('/', Path.DirectorySeparatorChar));
        }

        public static void DeleteAllEmptyDirAndMeta(ref List<DirectoryInfo> emptyDirs)
        {
            foreach (var dirInfo in emptyDirs)
            {
                var currentDirectory = Directory.GetCurrentDirectory();
                var path = GetRelativePath(dirInfo.FullName, currentDirectory);

                AssetDatabase.MoveAssetToTrash(path);
            }

            emptyDirs = null;
        }

        private static bool DirHasNoFile(DirectoryInfo dirInfo)
        {
            FileInfo[] files = null;

            try
            {
                files = dirInfo.GetFiles("*.*");
                files = files.Where(x => !IsMetaFile(x.Name)).ToArray();
            }
            catch (Exception)
            {
            }

            return files == null || files.Length == 0;
        }

        // return: Is this directory empty?
        private static bool WalkDirectoryTree(DirectoryInfo root, IsEmptyDirectory pred)
        {
            DirectoryInfo[] subDirs = root.GetDirectories();

            bool areSubDirsEmpty = true;
            foreach (DirectoryInfo dirInfo in subDirs)
            {
                if (false == WalkDirectoryTree(dirInfo, pred))
                    areSubDirsEmpty = false;
            }

            bool isRootEmpty = pred(root, areSubDirsEmpty);
            return isRootEmpty;
        }

        private static bool IsMetaFile(string path)
        {
            return path.EndsWith(".meta");
        }
    }
}