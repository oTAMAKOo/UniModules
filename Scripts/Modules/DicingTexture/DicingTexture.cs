
using System;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.Dicing
{
    public sealed class DicingTexture : ScriptableObject
	{
        //----- params -----

        private const string PixelHashFormat = "{0}-{1}";

        //----- field -----

        [SerializeField, ReadOnly]
		private int blockSize = 32;

		[SerializeField, ReadOnly]
		private int padding = 2;

        [SerializeField, ReadOnly]
        private bool hasAlphaMap = false;

        // 元テクスチャ情報.
        [SerializeField, ReadOnly]
        private DicingSourceData[] sourceData = null;

        // パックされたテクスチャ情報.
        [SerializeField, ReadOnly]
        private DicingBlockData[] blockData = null;

        [SerializeField, ReadOnly]
        private Texture2D texture = null;

        // テクスチャ名とブロック位置からピクセルハッシュを取得する為のディクショナリ.
        private Dictionary<string, Dictionary<string, ushort?>> pixelIdDictionary = null;

        // 同じピクセル情報のブロックは存在しないはずなのでディクショナリで高速アクセスできる.
        private Dictionary<ushort, DicingBlockData> blockByPixelId= null;

        //----- property -----

        public int BlockSize { get { return blockSize; } }

        public int Padding { get { return padding; } }

        public bool HasAlphaMap { get { return hasAlphaMap; } }
        
		public Texture2D Texture { get { return texture; } }

        //----- method -----

        private void Build()
        {
            if(pixelIdDictionary == null)
            {
                pixelIdDictionary = new Dictionary<string, Dictionary<string, ushort?>>();

                foreach (var data in sourceData)
                {
                    var blockDictionary = new Dictionary<string, ushort?>();

                    for (var y = 0; y < data.yblock; y++)
                    {
                        for (var x = 0; x < data.xblock; x++)
                        {
                            var key = string.Format(PixelHashFormat, x, y);
                            var id = data.blockIds[y * data.xblock + x];

                            blockDictionary.Add(key, id);
                        }
                    }

                    pixelIdDictionary.Add(data.textureName, blockDictionary);
                }
            }

            if (blockByPixelId == null)
            {
                blockByPixelId = blockData.ToDictionary(x => x.blockId);
            }
        }

        public void Set(Texture2D texture, int blockSize, int padding, DicingSourceData[] sourceData, DicingBlockData[] blockData, bool alphaMap)
        {
            this.texture = texture;
            this.blockSize = blockSize;
            this.padding = padding;
            this.sourceData = sourceData;
            this.blockData = blockData;
            this.hasAlphaMap = alphaMap;

            pixelIdDictionary = null;
            blockByPixelId = null;
        }

        public DicingSourceData[] GetAllDicingSource()
        {
            Build();

            return sourceData;
        }

        public DicingSourceData GetDicingSource(string textureName)
        {
            Build();

            return sourceData.FirstOrDefault(x => x.textureName == textureName);
        }

        public DicingBlockData GetBlockData(string textureName, int bx, int by)
        {
            Build();

            var texturePixelIds = pixelIdDictionary.GetValueOrDefault(textureName);

            if(texturePixelIds == null) { return null; }

            var key = string.Format(PixelHashFormat, bx, by);

            var pixelId = texturePixelIds.GetValueOrDefault(key, null);
            
            return pixelId.HasValue ? blockByPixelId.GetValueOrDefault(pixelId.Value) : null;
        }
    }
}
