
#if ENABLE_CRIWARE_ADX || ENABLE_CRIWARE_SOFDEC
﻿﻿
using UnityEngine;
using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.CriWare
{
    public enum CriAssetType
    {
        None = -1,
        Sound,
        Movie
    }

    [Serializable]
    public sealed class CriAssetInfo
    {
        [SerializeField]
        private string assetPath = null;
        [SerializeField]
        private CriAssetType assetType = CriAssetType.None;
        [SerializeField]
        private string hash = null;

        public string AssetPath { get { return assetPath; } }
        public CriAssetType AssetType { get { return assetType; } }
        public string Hash { get { return hash; } }

        public CriAssetInfo(CriAssetType assetType, string assetPath, string hash)
        {
            this.assetType = assetType;
            this.assetPath = assetPath;
            this.hash = hash;
        }
    }

    public sealed class CriAssetManageManifest : ScriptableObject
	{
        //----- params -----

        //----- field -----

        [SerializeField, ReadOnly]
        private CriAssetInfo[] soundAssets = new CriAssetInfo[0];
        [SerializeField, ReadOnly]
        private CriAssetInfo[] movieAssets = new CriAssetInfo[0];

        //----- property -----

        public CriAssetInfo[] AllAssets
        {
            get
            {
                return new CriAssetInfo[0]
                    .Concat(soundAssets)
                    .Concat(movieAssets)
                    .ToArray();
            }
        }

        public CriAssetInfo[] SoundAssets { get { return soundAssets; } }
        public CriAssetInfo[] MovieAssets { get { return movieAssets; } }

        //----- method -----

        public void UpdateManifest(CriAssetInfo[] assetInfos)
        {
            soundAssets = assetInfos.Where(x => x.AssetType == CriAssetType.Sound).ToArray();
            movieAssets = assetInfos.Where(x => x.AssetType == CriAssetType.Movie).ToArray();
        }

        public CriAssetInfo GetAssetInfo(CriAssetType type, string assetPath)
        {
            var path = assetPath.ToLower();

            var assetInfos = new CriAssetInfo[0];

            switch (type)
            {
                case CriAssetType.Sound:
                    assetInfos = soundAssets;
                    break;

                case CriAssetType.Movie:
                    assetInfos = movieAssets;
                    break;
            }

            return assetInfos.FirstOrDefault(x => path == x.AssetPath.ToLower());
        }
    }
}

#endif
