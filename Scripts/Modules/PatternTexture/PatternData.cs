
using UnityEngine;
using System;

namespace Modules.PatternTexture
{
    [Serializable]
    public sealed class PatternData
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private string textureName = null;
        [SerializeField]
        private string guid = null;
        [SerializeField]
        private long lastUpdate = 0;
        [SerializeField]
        private int width = 0;
        [SerializeField]
        private int height = 0;
        [SerializeField]
        private int xblock = 0;
        [SerializeField]
        private int yblock = 0;
        [SerializeField]
        private ushort[] blockIds = null;

        //----- property -----

        /// <summary> テクスチャ名 </summary>
        public string TextureName { get { return textureName; } }

        /// <summary> テクスチャGuid </summary>
        public string Guid { get { return guid; } }

        /// <summary> テクスチャ最終更新日 </summary>
        public long LastUpdate { get { return lastUpdate; } }

        /// <summary> テクスチャ幅 </summary>
        public int Width { get { return width; } }

        /// <summary> テクスチャ高さ </summary>
        public int Height { get { return height; } }

        /// <summary> Xブロック数 </summary>
        public int XBlock { get { return xblock; } }

        /// <summary> Yブロック数 </summary>
        public int YBlock { get { return yblock; } }

        /// <summary> ピクセルID </summary>
        public ushort[] BlockIds { get { return blockIds; } }

        //----- method -----

        public PatternData(string textureName, string guid, long lastUpdate, int width, int height, int xblock, int yblock, ushort[] blockIds)
        {
            this.textureName = textureName;
            this.guid = guid;
            this.lastUpdate = lastUpdate;
            this.width = width;
            this.height = height;
            this.xblock = xblock;
            this.yblock = yblock;
            this.blockIds = blockIds;
        }
    }
}
