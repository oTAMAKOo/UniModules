
#if ENABLE_CRIWARE_ADX

using System.IO;
using CriWare;
using Modules.CriWare;

namespace Modules.SoundManagement
{
    public sealed class SoundSheet
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
