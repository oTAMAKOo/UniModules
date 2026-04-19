
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.PatternTexture
{
    public sealed class PatternTexture : ScriptableObject
	{
        //----- params -----

		public enum TextureSizeType
		{
			/// <summary> 2のべき乗 </summary>
			PowerOf2,
			/// <summary> 4の倍数 </summary>
			MultipleOf4,
			/// <summary> 2のべき乗(正方形) </summary>
			SquarePowerOf2,
			/// <summary> 4の倍数(正方形) </summary>
			SquareMultipleOf4,
		}

		//----- field -----

		[SerializeField, ReadOnly]
		private TextureSizeType sizeType = TextureSizeType.MultipleOf4;

        [SerializeField, ReadOnly]
		private int blockSize = 32;

		[SerializeField, ReadOnly]
		private int filterPixels = 2;

        [SerializeField, ReadOnly]
        private bool hasAlphaMap = false;

        // 元テクスチャ情報.
        [SerializeField, ReadOnly]
        private PatternData[] sourceData = null;

        // パックされたテクスチャ情報.
        [SerializeField, ReadOnly]
        private PatternBlockData[] blockData = null;

        [SerializeField, ReadOnly]
        private Texture2D texture = null;

        // テクスチャ名からパターン情報を取得する為のディクショナリ.
        private Dictionary<string, PatternData> patternDataByName = null;

        // 同じピクセル情報のブロックは存在しないはずなのでディクショナリで高速アクセスできる.
        private Dictionary<ushort, PatternBlockData> blockByPixelId = null;

        //----- property -----

		public TextureSizeType SizeType { get { return sizeType; } }

        public int BlockSize { get { return blockSize; } }

		public int FilterPixels { get { return filterPixels; } }

        public bool HasAlphaMap { get { return hasAlphaMap; } }

		public Texture2D Texture { get { return texture; } }

        //----- method -----

        private void Build()
        {
            if (patternDataByName == null)
            {
                patternDataByName = sourceData.ToDictionary(x => x.TextureName);
            }

            if (blockByPixelId == null)
            {
                blockByPixelId = blockData.ToDictionary(x => x.blockId);
            }
        }

        public void Set(Texture2D texture, TextureSizeType sizeType, int blockSize, int filterPixels,
						PatternData[] sourceData, PatternBlockData[] blockData, bool hasAlphaMap)
        {
            this.texture = texture;
			this.sizeType = sizeType;
            this.blockSize = blockSize;
			this.filterPixels = filterPixels;
            this.sourceData = sourceData;
            this.blockData = blockData;
            this.hasAlphaMap = hasAlphaMap;

            patternDataByName = null;
            blockByPixelId = null;
        }

        public IReadOnlyList<PatternData> GetAllPatternData()
        {
            Build();

            return sourceData;
        }

        public PatternData GetPatternData(string textureName)
        {
            if (string.IsNullOrEmpty(textureName)){ return null; }

            Build();

            return patternDataByName.GetValueOrDefault(textureName);
        }

        public PatternBlockData GetBlockData(string textureName, int bx, int by)
        {
            var patternData = GetPatternData(textureName);

            return GetBlockData(patternData, bx, by);
        }

        public PatternBlockData GetBlockData(PatternData patternData, int bx, int by)
        {
            if (patternData == null){ return null; }

            Build();

            var index = by * patternData.XBlock + bx;

            if (index < 0 || patternData.BlockIds.Length <= index){ return null; }

            var blockId = patternData.BlockIds[index];

            return blockByPixelId.GetValueOrDefault(blockId);
        }

		public int GetBlockCount()
		{
			Build();

			return blockByPixelId.Count;
		}
    }
}
