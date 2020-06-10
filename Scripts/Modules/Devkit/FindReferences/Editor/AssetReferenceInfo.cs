﻿﻿
using System.Collections.Generic;

using Object = UnityEngine.Object;

namespace Modules.Devkit.FindReferences
{
    public sealed class AssetReferenceInfo
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public string TargetPath { get; private set; }

        public Object Target { get; private set; }

        public List<string> Dependencies { get; private set; }

        //----- method -----

        public AssetReferenceInfo(string targetPath, Object target)
        {
            this.TargetPath = targetPath;
            this.Target = target;
            this.Dependencies = new List<string>();
        }
    }
}
