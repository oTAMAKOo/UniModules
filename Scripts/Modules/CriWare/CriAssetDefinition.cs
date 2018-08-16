﻿
#if ENABLE_CRIWARE
﻿﻿
using UnityEngine;
using Unity.Linq;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;
using Extensions;

namespace Modules.CriWare
{
    public static class CriAssetDefinition
	{
        public const string CriAssetFolder = "CriWare";

        public const string VersionFileExtension = ".ver";

        public const string AcfExtension = ".acf";
        public const string AcbExtension = ".acb";
        public const string AwbExtension = ".awb";
        public const string UsmExtension = ".usm";

        public static string[] AssetAllExtensions
        {
            get
            {
                return new string[]
                {
                    AcfExtension, AcbExtension, AwbExtension, UsmExtension
                };
            }
        }
    }
}

#endif