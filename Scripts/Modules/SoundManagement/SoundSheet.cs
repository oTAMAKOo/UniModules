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
using Modules.CriWare;

namespace Modules.SoundManagement
{
    public class SoundSheet
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public string AssetPath { get; private set; }
        public CriAtomExAcb Acb { get; private set; }

        //----- method -----

        public SoundSheet(string assetPath)
        {
            AssetPath = assetPath;
        }

        public SoundSheet(string assetPath, CriAtomExAcb acb) : this(assetPath)
        {
            Acb = acb;
        }

        public static string AcbPath(string assetPath) { return Path.ChangeExtension(assetPath, CriAssetDefinition.AcbExtension); }
        public static string AwbPath(string assetPath) { return Path.ChangeExtension(assetPath, CriAssetDefinition.AwbExtension); }
    }    
}

#endif