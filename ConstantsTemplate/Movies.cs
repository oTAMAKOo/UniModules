
#if UNIMODULE_CONSTANTS_TEMPLATE

#if ENABLE_CRIWARE_SOFDEC

using System.Collections.Generic;
using CriWare;
using Extensions;

namespace Modules.Movie
{
    public static partial class Movies
	{
        public enum Mana
        {

        }

        private static Dictionary<Mana, ManaInfo> internalMovies = new Dictionary<Mana, ManaInfo>()
        {

        };

        public static ManaInfo GetManaInfo(Mana mana)
        {
            var fileDirectory = string.Empty;

            #if UNITY_EDITOR

            var editorStreamingAssetsFolderPath = "Assets/StreamingAssets";

            fileDirectory = UnityPathUtility.ConvertAssetPathToFullPath(editorStreamingAssetsFolderPath);

            #else

            fileDirectory = Common.streamingAssetsPath;

            #endif

            var info = internalMovies.GetValueOrDefault(mana);

            var path = PathUtility.Combine(fileDirectory, info.UsmPath);

            return new ManaInfo(path);
        }
    }
}

#endif

#endif
