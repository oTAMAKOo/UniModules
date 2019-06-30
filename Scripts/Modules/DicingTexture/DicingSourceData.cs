using System;

namespace Modules.Dicing
{
    [Serializable]
    public sealed class DicingSourceData
    {
        // テクスチャ名.
        public string textureName;
        
        // テクスチャGuid.
        public string guid;
        
        // テクスチャ最終更新日.
        public long lastUpdate;
        
        // サイズ.
        public int width;
        public int height;

        // ブロック数.
        public int xblock;
        public int yblock;

        // ピクセルID.
        public ushort[] blockIds;
    }
}
