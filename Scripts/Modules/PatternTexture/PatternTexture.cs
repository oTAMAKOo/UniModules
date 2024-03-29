
using UnityEngine;
using System;
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

        // テクスチャ名とブロック位置からピクセルハッシュを取得する為のディクショナリ.
        private Dictionary<string, Dictionary<Tuple<int, int>, ushort?>> pixelIdDictionary = null;

        // 同じピクセル情報のブロックは存在しないはずなのでディクショナリで高速アクセスできる.
        private Dictionary<ushort, PatternBlockData> blockByPixelId= null;

		// ブロック数.
		private int? blockCount = null;

        //----- property -----

		public TextureSizeType SizeType { get { return sizeType; } }

        public int BlockSize { get { return blockSize; } }

		public int FilterPixels { get { return filterPixels; } }

        public bool HasAlphaMap { get { return hasAlphaMap; } }
        
		public Texture2D Texture { get { return texture; } }

        //----- method -----

        private void Build()
        {
            if(pixelIdDictionary == null)
            {
                pixelIdDictionary = new Dictionary<string, Dictionary<Tuple<int, int>, ushort?>>();

                foreach (var data in sourceData)
                {
                    var blockDictionary = new Dictionary<Tuple<int, int>, ushort?>();

                    for (var y = 0; y < data.YBlock; y++)
                    {
                        for (var x = 0; x < data.XBlock; x++)
                        {
                            var key = Tuple.Create(x, y);
                            var id = data.BlockIds[y * data.XBlock + x];

                            blockDictionary.Add(key, id);
                        }
                    }

                    pixelIdDictionary.Add(data.TextureName, blockDictionary);
                }
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

            pixelIdDictionary = null;
            blockByPixelId = null;
			blockCount = null;
        }

        public IReadOnlyList<PatternData> GetAllPatternData()
        {
            Build();

            return sourceData;
        }

        public PatternData GetPatternData(string textureName)
        {
            Build();

            return sourceData.FirstOrDefault(x => x.TextureName == textureName);
        }

        public PatternBlockData GetBlockData(string textureName, int bx, int by)
        {
            Build();

            var texturePixelIds = pixelIdDictionary.GetValueOrDefault(textureName);

            if(texturePixelIds == null) { return null; }

            var key = Tuple.Create(bx, by);

            var pixelId = texturePixelIds.GetValueOrDefault(key, null);
            
            return pixelId.HasValue ? blockByPixelId.GetValueOrDefault(pixelId.Value) : null;
        }

		public int GetBlockCount()
		{
			Build();

			if (!blockCount.HasValue)
			{
				blockCount = blockByPixelId.Count;
			}

			return blockCount.Value;
		}
    }
}
