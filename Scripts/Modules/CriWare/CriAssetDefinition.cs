
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC
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
        public const string VersionFileExtension = ".ver";

        public const string AcfExtension = ".acf";
        public const string AcbExtension = ".acb";
        public const string AwbExtension = ".awb";
        public const string UsmExtension = ".usm";
        
        public static readonly string[] AssetAllExtensions = new string[]
        {
            AcfExtension, AcbExtension, AwbExtension, UsmExtension
        };
    }
}

#endif
