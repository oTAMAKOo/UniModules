
using System;
using Extensions;

namespace Modules.PatternTexture
{
    [Serializable]
    public sealed class PatternBlockData
    {
        // ピクセル開始位置.
        public int x = 0;
        public int y = 0;

        // 幅、高さ.
        public int w = 0;
        public int h = 0;
        
        // ブロック内のアルファマップ.
        public byte[] alphaMap = null;

        // ブロックのピクセルID.
        public ushort blockId = 0;

        [NonSerialized]
        private byte[] decompressedAlphaMap = null;

        /// <summary>
        /// アルファ値があるピクセルか.
        /// </summary>
        public bool HasAlpha(int x, int y)
        {
            if (alphaMap.IsEmpty()) { return false; }

            if (decompressedAlphaMap == null)
            {
                decompressedAlphaMap = alphaMap.Decompress();
            }

            var index = (x + y * w) / 8;
            var bit = (x + y * w) % 8;

            if (decompressedAlphaMap.Length <= index) { return false; }

            var byteData = decompressedAlphaMap[index];

            return (byteData & 0x01 << bit) != 0;
        }
    }
}
