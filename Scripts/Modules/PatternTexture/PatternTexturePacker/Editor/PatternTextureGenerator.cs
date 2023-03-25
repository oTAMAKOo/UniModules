
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Extensions;
using Extensions.Devkit;

namespace Modules.PatternTexture
{
	public sealed class PatternTextureData
    {
        public PatternData[] PatternData { get; set; }
        public PatternBlockData[] PatternBlocks { get; set; }
        public Texture2D Texture { get; set; }
    }

    public sealed class PatternTextureGenerator
    {
		//----- params -----
        
        private sealed class PatternTargetData
        {
            public Texture2D texture;
            public int bx;
            public int by;
            public TextureBlock[] blocks;
        }

        private sealed class TextureBlock
        {
            public ushort blockId;
            public int width;
            public int height;
            public Color32[] colors;
            public bool isAllTransparent;
        }

        //----- field -----

        // ブロックに割り振るID.
        private ushort blockId = 0;
        // ブロックのハッシュ情報とブロックIDのテーブル.
        private Dictionary<string, ushort?> blockIdbyHashCode = null;

        //----- property -----

        //----- method -----

        public PatternTextureData Generate(string exportPath, int blockSize, int padding, int filterPixels, 
											PatternTexture.TextureSizeType sizeType,Texture2D[] sourceTextures, bool hasAlphaMap)
        {
            var patternTextureData = new PatternTextureData();
			
			try
			{
				blockId = 0;
				blockIdbyHashCode = new Dictionary<string, ushort?>();

				// 補間用ピクセル分のパディングを追加.
				padding += filterPixels * 2;

				// テクスチャ情報読み込み.

				var directory = Path.GetDirectoryName(exportPath);
				var textureName = Path.ChangeExtension(Path.GetFileName(exportPath), ".png");
				var texturePath = PathUtility.Combine(directory, textureName);

				using (new AssetEditingScope())
				{
					var changed = false;

					foreach (var sourceTexture in sourceTextures)
					{
						changed |= SetTextureEditable(sourceTexture);
					}

					if (changed)
					{
						AssetDatabase.Refresh();
					}
				}

				var patternTargetDatas = ReadTextureBlock(blockSize, sourceTextures);

				// 書き込み用テクスチャ作成.
				var texture = CreateTexture(sizeType, blockSize, padding, patternTargetDatas);

				// パターン情報構築.
				patternTextureData.PatternData = BuildPatternData(patternTargetDatas);

				// テクスチャにピクセル情報を書き込み.
				patternTextureData.PatternBlocks = BitBlockTransfer(texture, blockSize, padding, filterPixels, patternTargetDatas, hasAlphaMap);

				// ファイルに書き込み.

				File.WriteAllBytes(texturePath, texture.EncodeToPNG());

				AssetDatabase.ImportAsset(texturePath);

				patternTextureData.Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			return patternTextureData;
        }

        private bool SetTextureEditable(Texture texture)
        {
            var changed = false;

            var assetPath = AssetDatabase.GetAssetPath(texture);

            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (!textureImporter.isReadable)
            {
                textureImporter.isReadable = true;
                changed = true;
            }

            if (textureImporter.textureCompression != TextureImporterCompression.Uncompressed)
            {
                textureImporter.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (changed)
            {
                textureImporter.SaveAndReimport();
            }
            
            return changed;
        }

		// テクスチャを読み込み.
        private PatternTargetData[] ReadTextureBlock(int blockSize, Texture2D[] textures)
        {
            var list = new List<PatternTargetData>();

			for (var i = 0; i < textures.Length; i++)
			{
				var texture = textures[i];

				var assetPath = AssetDatabase.GetAssetPath(texture);

                var blockList = new List<TextureBlock>();

                var bx = texture.width / blockSize;
                var by = texture.height / blockSize;

                var pixels = texture.GetPixels32();

                // 残りの領域があった場合追加.
                bx += texture.width % blockSize != 0 ? 1 : 0;
                by += texture.height % blockSize != 0 ? 1 : 0;

				var count = 0;
				var totalCount = bx * by;

				var title = $"ReadTextureBlock ({i+1}/{textures.Length+1})";

                for (var y = 0; y < by; y++)
                {
                    for (var x = 0; x < bx; x++)
                    {
						EditorUtility.DisplayProgressBar(title, assetPath, (float)count / totalCount);

                        var block = GetTextureBlock(texture, x, y, blockSize, pixels);

                        blockList.Add(block);

						count++;
                    }
                }

                list.Add(new PatternTargetData() { texture = texture, bx = bx, by = by, blocks = blockList.ToArray() });
            }

			EditorUtility.ClearProgressBar();

            return list.ToArray();
        }

		private Texture2D CreateTexture(PatternTexture.TextureSizeType sizeType, int blockSize, int padding, PatternTargetData[] patternTargetDatas)
		{
			EditorUtility.DisplayProgressBar("CreateTexture", "Creating empty texture", 0f);

			// 透明ピクセル、同じピクセル情報のブロックは対象外.
			var totalBlockCount = patternTargetDatas
				.SelectMany(x => x.blocks)
				.Select(x => x.blockId)
				.Distinct()
				.Count();

			var textureSize = CalcRequireTextureSize(sizeType, blockSize, padding, totalBlockCount);

			var texture = TextureUtility.CreateEmptyTexture(textureSize.x, textureSize.y);

			EditorUtility.ClearProgressBar();
			
			return texture;
		}

        // 2のべき乗で全ブロックを格納できるテクスチャサイズを計算.
        private Vector2Int CalcRequireTextureSize(PatternTexture.TextureSizeType sizeType, int blockSize, int padding, int totalBlockCount)
        {
            var textureSize = new Vector2Int(2, 2);

            // パディングの分も含める.
            var totalBlockSize = blockSize + padding;

			// 1辺に格納予定のブロック数.
			var lineBlock = Math.Ceiling(Math.Sqrt(totalBlockCount));

			// テクスチャサイズ.

			var requireWidth = padding + lineBlock * totalBlockSize;

			switch (sizeType)
			{
				case PatternTexture.TextureSizeType.PowerOf2:
					{
						var size_x = 2;

						while (true)
						{
							if(requireWidth < size_x){ break; }

							size_x *= 2;
						}

						var size_y = 2;

						var xLineBlock = (size_x - padding) / totalBlockSize;
						var requireHight = padding + (totalBlockCount / xLineBlock + 1) * totalBlockSize;

						while (true)
						{
							if(requireHight < size_y){ break; }

							size_y *= 2;
						}

						textureSize = new Vector2Int(size_x, size_y);
					}
					break;

				case PatternTexture.TextureSizeType.MultipleOf4:
					{
						var size_x = 4;

						while (true)
						{
							if(requireWidth < size_x){ break; }

							size_x += 4;
						}

						var size_y = 4;

						var xLineBlock = (size_x - padding) / totalBlockSize;
						var requireHight = padding + (totalBlockCount / xLineBlock + 1) * totalBlockSize;

						while (true)
						{
							if(requireHight < size_y){ break; }

							size_y += 4;
						}

						textureSize = new Vector2Int(size_x, size_y);
					}
					break;

				case PatternTexture.TextureSizeType.SquarePowerOf2:
					{
						var size = 2;

						while (true)
						{
							if(requireWidth < size){ break; }

							size *= 2;
						}

						textureSize = new Vector2Int(size, size);
					}
					break;

				case PatternTexture.TextureSizeType.SquareMultipleOf4:
					{
						var size = 4;

						while (true)
						{
							if(requireWidth < size){ break; }

							size += 4;
						}

						textureSize = new Vector2Int(size, size);
					}
					break;
			}

            return textureSize;
        }

        // 元になったテクスチャ情報構築.
        private PatternData[] BuildPatternData(PatternTargetData[] targetDatas)
        {
            var result = new List<PatternData>();

			for (var i = 0; i < targetDatas.Length; i++)
			{
				var targetData = targetDatas[i];

                var texture = targetData.texture;
                var assetPath = AssetDatabase.GetAssetPath(texture);
                var fullPath = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);
             
				EditorUtility.DisplayProgressBar("BuildPatternData", assetPath, (float)i / targetDatas.Length);

                var textureName = Path.GetFileNameWithoutExtension(assetPath);
                var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                var lastUpdate = File.GetLastWriteTime(fullPath).ToUnixTime();
                var width = texture.width;
                var height = texture.height;
                var blockIds = targetData.blocks.Select(x => x.blockId).ToArray();

                var item = new PatternData(textureName, assetGuid, lastUpdate, width, height, targetData.bx, targetData.by, blockIds);

                result.Add(item);
            }

            return result.OrderBy(x => x.TextureName, new NaturalComparer()).ToArray();
        }

        // 抽出した差分データを書き出し.
        private PatternBlockData[] BitBlockTransfer(Texture2D texture, int blockSize, int padding, int filterPixels, PatternTargetData[] patternTargetDatas, bool hasAlphaMap)
        {
            var blockDataDictionary = new Dictionary<int, PatternBlockData>();

            var x_start = padding;
            var y_start = padding;

            var transX = x_start;
            var transY = y_start;

            for (var i = 0; i < patternTargetDatas.Length; i++)
            {
                var fileName = Path.GetFileName(AssetDatabase.GetAssetPath(patternTargetDatas[i].texture));
                var message = string.Format("Writing texture {0} ...", fileName);

                EditorUtility.DisplayProgressBar("Please wait...", message, (float)i / patternTargetDatas.Length);

                foreach (var item in patternTargetDatas[i].blocks)
                {
                    if (blockDataDictionary.ContainsKey(item.blockId)){ continue; }

                    if (texture.width < transX + item.width + padding)
                    {
                        transX = x_start;
                        transY += blockSize + padding;
                    }

					try
					{
						texture.SetPixels32(transX, transY, item.width, item.height, item.colors);
					}
					catch
					{
						EditorUtility.ClearProgressBar();
						throw;
					}

					// 外周に追加のピクセルを追加.
					InsertPixelsForFilterMode(texture, transX, transY, item.width, item.height, filterPixels);

                    // アルファ値情報生成.
                    var alphaMap = !hasAlphaMap || item.isAllTransparent ? 
                        new byte[0] : 
                        BuildAlphaMap(item.width, item.height, item.colors);

                    // 圧縮.
                    var compressedAlphaMap = alphaMap.Compress(); 

                    // ブロック情報を追加.
                    var blockData = new PatternBlockData()
                    {
                        x = transX,
                        y = transY,
                        w = item.width,
                        h = item.height,
                        alphaMap = compressedAlphaMap,
                        blockId = item.blockId,
						isAllTransparent = item.isAllTransparent,
                    };

                    blockDataDictionary.Add(item.blockId, blockData);

                    // 次の位置.
                    transX += item.width + padding;
                }
            }

            EditorUtility.ClearProgressBar();

            texture.Apply(true, false);

            return blockDataDictionary.Values.ToArray();
        }

        // 補完時に周りのピクセル情報を巻き込まないように外周にピクセルを追加.
        private void InsertPixelsForFilterMode(Texture2D texture, int x, int y, int width, int height, int filterPixels)
        {
			// 実装用の表示テスト用.
			var addPixelDebug = false;

			// カラー取得関数.
			Color GetAddPixelColor(int _x, int _y)
			{
				var color = texture.GetPixel(_x, _y);

				if (addPixelDebug) { color.a *= 0.25f; }

				return color;
			}

            // Top / Bottom.
            for (var px = x - 1; px < x + width + 1; px++)
            {
                // Top.
				{
	                var pixel = GetAddPixelColor(px, y);

					for (var i = 1; i < filterPixels + 1; i++)
					{
						texture.SetPixel(px, y - i, pixel);
					}
				}

                // Bottom.
				{
					var pixel = GetAddPixelColor(px, y + height - 1);
					
					for (var i = 0; i < filterPixels; i++)
					{
		                texture.SetPixel(px, y + height + i, pixel);
					}
				}
			}

            // Left / Right.
            for (var py = y - 1; py < y + height + 1; py++)
            {
                // Left.
				{
					var pixel = GetAddPixelColor(x, py);

					for (var i = 1; i < filterPixels + 1; i++)
					{
		                texture.SetPixel(x - i, py, pixel);
					}
				}

                // Right.
				{
					var pixel = GetAddPixelColor(x + width - 1, py);

					for (var i = 0; i < filterPixels; i++)
					{
						texture.SetPixel(x + width + i, py, pixel);
					}
				}
			}
        }

        private TextureBlock GetTextureBlock(Texture2D texture, int x, int y, int blockSize, Color32[] pixels)
        {
            var blockWidth = x * blockSize + blockSize <= texture.width ? blockSize : texture.width - x * blockSize;
            var blockHeight = y * blockSize + blockSize <= texture.height ? blockSize : texture.height - y * blockSize;

            var colors = GetBlockColors(x * blockSize, y * blockSize, blockWidth, blockHeight, texture.width, pixels);
           
            // アルファ値0のピクセルの色を統一.
            for (var i = 0; i < colors.Length; i++)
            {
                if(colors[i].a == 0f)
                {
                    colors[i] = new Color32(0, 0, 0, 0);
                }
            }
            
            var isAllTransparent = colors.All(c => c.a == 0f);
            var colorHash = GetPixelColorsHash(blockWidth, blockHeight, blockSize, colors);
            var blockId = GetHashId(colorHash);

            var blockData = new TextureBlock()
            {
                width = blockWidth,
                height = blockHeight,
                colors = colors,
                isAllTransparent = isAllTransparent,
                blockId = blockId,
            };

            return blockData;
        }

        private ushort GetHashId(string hash)
        {
            var id = blockIdbyHashCode.GetValueOrDefault(hash, null);

            if (!id.HasValue)
            {
                id = blockId;

                blockIdbyHashCode.Add(hash, blockId);
                blockId++;
            }

            return id.Value;
        }

        private Color32[] GetBlockColors(int x, int y, int width, int height, int textureWidth, Color32[] colors)
        {
            var blockColors = new List<Color32>();

            for (var py = y; py < y + height; py++)
            {
                for (var px = x; px < x + width; px++)
                {
                    blockColors.Add(colors[py * textureWidth + px]);
                }
            }

            return blockColors.ToArray();
        }

        private byte[] BuildAlphaMap(int width, int height, Color32[] colors)
        {
            var alphaMap = new List<byte>();

            var bit = 0;
            var alphaBit = new byte();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    // 0x00だとゴミピクセルを拾うことがあるので0x01以上で判定.
                    var hasAlpha = 0x01 < colors[y * width + x].a ? 0x01 : 0x00;

                    alphaBit |= (byte)(hasAlpha << bit);

                    bit++;

                    if (bit == 8)
                    {
                        alphaMap.Add(alphaBit);

                        bit = 0;
                        alphaBit = 0;
                    }
                }
            }

            return alphaMap.ToArray();
        }

        private string GetPixelColorsHash(int blockWidth, int blockHeight, int blockSize, Color32[] pixelColors)
        {
            // 指定ブロックサイズ＋全ピクセルが透過の場合の短縮Hash値.
            const string BlockAllTransparent = "0"; 

            const string format = "{0}x{1}:{2}";

            // 既定サイズ＋全ピクセルが透過の場合はハッシュ値を使用しない.
            if (pixelColors.All(x => x.a == 0f) && blockWidth == blockSize && blockHeight == blockSize)
            {
                return BlockAllTransparent;
            }

            var builder = new StringBuilder();

            foreach (var color in pixelColors)
            {
                var hex = color.ColorToHex(true);
                builder.Append(hex);
            }

            var str = builder.ToString();

            // 色情報は量が多いのでハッシュ化.
            return string.Format(format, blockWidth, blockHeight, str).GetCRC();
        }
    }
}
