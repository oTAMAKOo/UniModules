﻿﻿﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;

namespace Modules.Devkit.CleanDirectory
{
	public class SaveCleanDirectory : UnityEditor.AssetModificationProcessor
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static string[] OnWillSaveAssets(string[] paths)
        {
            if (CleanDirectoryUtility.Prefs.cleanOnSave)
            {
                List<DirectoryInfo> emptyDirs;

                CleanDirectoryUtility.FillEmptyDirList(out emptyDirs);

                if (emptyDirs != null && emptyDirs.Count > 0)
                {
                    CleanDirectoryUtility.DeleteAllEmptyDirAndMeta(ref emptyDirs);

                    Debug.Log("[Clean] Cleaned Empty Directories on Save");
                }
            }

            return paths;
        }
    }
}