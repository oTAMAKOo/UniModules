
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.ValidateAsset.TextureSize
{
    [Serializable]
    public sealed class ValidateData
    {
        [SerializeField]
        public string folderGuid = null;
        [SerializeField]
        public int width = 0;
        [SerializeField]
        public int heigth = 0;
    }

    public sealed class TextureSizeValidateConfig : ReloadableScriptableObject<TextureSizeValidateConfig>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private ValidateData[] validateData = null;
        [SerializeField]
        private string[] ignoreGuids = null;
        [SerializeField]
        private string[] ignoreFolderNames = null;

        //----- property -----

        //----- method -----

        public ValidateData[] GetValidateData()
        {
            return validateData;
        }

        public HashSet<string> GetIgnoreGuids()
        {
            return ignoreGuids.ToHashSet();
        }

        public HashSet<string> GetIgnoreFolderNames()
        {
            return ignoreFolderNames.ToHashSet();
        }
    }
}