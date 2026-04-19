
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
        // ブロックのハッシュ情報とブロックIDのテーブル(ハッシュ衝突に備えて同一ハッシュの複数ブロックをList保持).
        private Dictionary<string, List<(ushort blockId, Color32[] colors)>> blockIdByHashCode = null;
        // テクスチャインポート設定を一時的に変更する前の値を保持.
        private Dictionary<string, (bool isReadable, TextureImporterCompression compression)> originalTextureSettings = null;

        //----- property -----

        //----- method -----

        public PatternTextureData Generate(string exportPath, int blockSize, int padding, int filterPixels,
											PatternTexture.TextureSizeType sizeType,Texture2D[] sourceTextures, bool hasAlphaMap)
        {
			// テクスチャ名重複検知.
			var duplicateNames = sourceTextures
				.Select(x => x.name)
				.GroupBy(x => x)
				.Where(g => 1 < g.Count())
				.Select(g => g.Key)
				.ToArray();

			if (duplicateNames.Any())
			{
				Debug.LogError($"Duplicate texture names detected. Cannot generate PatternTexture.\n{string.Join(", ", duplicateNames)}");
				return null;
			}

            var patternTextureData = new PatternTextureData();

			Texture2D temporaryTexture = null;

			try
			{
				blockId = 0;
				blockIdByHashCode = new Dictionary<string, List<(ushort, Color32[])>>();
				originalTextureSettings = new Dictionary<string, (bool, TextureImporterCompression)>();

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
				temporaryTexture = CreateTexture(sizeType, blockSize, padding, filterPixels, patternTargetDatas);

				// パターン情報構築.
				patternTextureData.PatternData = BuildPatternData(patternTargetDatas);

				// テクスチャにピクセル情報を書き込み.
				patternTextureData.PatternBlocks = BitBlockTransfer(temporaryTexture, blockSize, padding, filterPixels, patternTargetDatas, hasAlphaMap);

				// ファイルに書き込み.

				File.WriteAllBytes(texturePath, temporaryTexture.EncodeToPNG());

				AssetDatabase.ImportAsset(texturePath);

				patternTextureData.Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
			}
			finally
			{
				EditorUtility.ClearProgressBar();

				// 一時テクスチャを破棄.
				if (temporaryTexture != null)
				{
					UnityEngine.Object.DestroyImmediate(temporaryTexture);
				}

				// テクスチャインポート設定を元に戻す.
				RestoreTextureSettings();
			}

			return patternTextureData;
        }

        private bool SetTextureEditable(Texture texture)
        {
            var changed = false;

            var assetPath = AssetDatabase.GetAssetPath(texture);

            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (textureImporter == null){ return false; }

            // 変更前の値を保持.
            if (!originalTextureSettings.ContainsKey(assetPath))
            {
                originalTextureSettings.Add(assetPath, (textureImporter.isReadable, textureImporter.textureCompression));
            }

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

        private void RestoreTextureSettings()
        {
            if (originalTextureSettings == null){ return; }

            using (new AssetEditingScope())
            {
                var changed = false;

                foreach (var entry in originalTextureSettings)
                {
                    var textureImporter = AssetImporter.GetAtPath(entry.Key) as TextureImporter;

                    if (textureImporter == null){ continue; }

                    var modified = false;

                    if (textureImporter.isReadable != entry.Value.isReadable)
                    {
                        textureImporter.isReadable = entry.Value.isReadable;
                        modified = true;
                    }

                    if (textureImporter.textureCompression != entry.Value.compression)
                    {
                        textureImporter.textureCompression = entry.Value.compression;
                        modified = true;
                    }

                    if (modified)
                    {
                        textureImporter.SaveAndReimport();
                        changed = true;
                    }
                }

                if (changed)
                {
                    AssetDatabase.Refresh();
                }
            }

            originalTextureSettings.Clear();
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

				var title = $"ReadTextureBlock ({i+1}/{textures.Length})";

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

		private Texture2D CreateTexture(PatternTexture.TextureSizeType sizeType, int blockSize, int padding, int filterPixels, PatternTargetData[] patternTargetDatas)
		{
			EditorUtility.DisplayProgressBar("CreateTexture", "Creating empty texture", 0f);

			// 透明ピクセル、同じピクセル情報のブロックは対象外.
			var totalBlockCount = patternTargetDatas
				.SelectMany(x => x.blocks)
				.Select(x => x.blockId)
				.Distinct()
				.Count();

			var textureSize = CalcRequireTextureSize(sizeType, blockSize, padding, filterPixels, totalBlockCount);

			var texture = TextureUtility.CreateEmptyTexture(textureSize.x, textureSize.y);

			EditorUtility.ClearProgressBar();
			
			return texture;
		}
		
		/// <summary> テクスチャサイズを計算 </summary>
        private Vector2Int CalcRequireTextureSize(PatternTexture.TextureSizeType sizeType, int blockSize, int padding, int filterPixels, int totalBlockCount)
        {
            var textureSize = new Vector2Int(2, 2);

			// ブロックで使用するサイズ.
            var totalBlockSize = blockSize + filterPixels * 2 + padding;

			// 1辺に格納予定のブロック数.
			var lineBlock = Math.Ceiling(Math.Sqrt(totalBlockCount)) + 1;

			// テクスチャサイズ.

			var requireWidth = padding + lineBlock * totalBlockSize;

			switch (sizeType)
			{
				case PatternTexture.TextureSizeType.PowerOf2:
					{
						var size_x = 2;

						while (size_x <= requireWidth){ size_x *= 2; }

						var xLineBlock = (size_x - padding) / totalBlockSize;
						var yLineBlock = Math.Ceiling((float)totalBlockCount / xLineBlock);

						var requireHeight = padding + yLineBlock * totalBlockSize;

						var size_y = 2;

						while (size_y <= requireHeight){ size_y *= 2; }

						textureSize = new Vector2Int(size_x, size_y);
					}
					break;

				case PatternTexture.TextureSizeType.MultipleOf4:
					{
						var size_x = 4;

						while (size_x <= requireWidth){ size_x += 4; }

						var xLineBlock = (size_x - padding) / totalBlockSize;
						var yLineBlock = Math.Ceiling((float)totalBlockCount / xLineBlock);

						var requireHeight = padding + yLineBlock * totalBlockSize;

						var size_y = 4;

						while (size_y <= requireHeight){ size_y += 4; }

						textureSize = new Vector2Int(size_x, size_y);
					}
					break;

				case PatternTexture.TextureSizeType.SquarePowerOf2:
					{
						var size = 2;

						while (size <= requireWidth){ size *= 2; }

						textureSize = new Vector2Int(size, size);
					}
					break;

				case PatternTexture.TextureSizeType.SquareMultipleOf4:
					{
						var size = 4;

						while (size <= requireWidth){ size += 4; }

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

            return result.OrderBy(x => AssetDatabase.GUIDToAssetPath(x.Guid), new NaturalComparer()).ToArray();
        }

        // 抽出した差分データを書き出し.
        private PatternBlockData[] BitBlockTransfer(Texture2D texture, int blockSize, int padding, int filterPixels, PatternTargetData[] patternTargetDatas, bool hasAlphaMap)
        {
            var blockDataDictionary = new Dictionary<int, PatternBlockData>();

			var totalBlockSize = blockSize + filterPixels * 2;

            var x_start = padding + filterPixels;
            var y_start = padding + filterPixels;

            var transX = x_start;
            var transY = y_start;

			var xCount = 0;
			var yCount = 0;

			for (var i = 0; i < patternTargetDatas.Length; i++)
            {
                var fileName = Path.GetFileName(AssetDatabase.GetAssetPath(patternTargetDatas[i].texture));
                var message = string.Format("Writing texture {0} ...", fileName);

                EditorUtility.DisplayProgressBar("Please wait...", message, (float)i / patternTargetDatas.Length);

				foreach (var item in patternTargetDatas[i].blocks)
                {
                    if (blockDataDictionary.ContainsKey(item.blockId)){ continue; }

					// x_startに最初に足している分を引く.
                    if (texture.width < transX + totalBlockSize - filterPixels)
                    {
						transX = x_start;
                        transY += padding + totalBlockSize;

						xCount = 0;
						yCount++;
					}

					try
					{
						texture.SetPixels32(transX, transY, item.width, item.height, item.colors);
					}
					catch
					{
						using (new DisableStackTraceScope())
						{
							var logTexture = $"texture: {texture.width}x{texture.height}";
							var logTrans = $"trans: x = {transX} y = {transY} width = {item.width} height = {item.height}";
							var logCount = $"count: x = {xCount} y = {yCount}";

							Debug.LogError($"SetPixel Error:\n{logTexture}\n{logTrans}\n{logCount}");
						}

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
                    transX += padding + totalBlockSize;

					xCount++;
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
			
			// ピクセル色取得関数.
			Color GetAddPixelColor(int _x, int _y)
			{
				var color = texture.GetPixel(_x, _y);

				if (addPixelDebug)
				{
					color *= Color.magenta;
				}

				return color;
			}

			// Top / Bottom.
            for (var px = x; px < x + width; px++)
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
            for (var py = y; py < y + height; py++)
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

			// TopLeft.
			{
				for (var px = x - 1; x - filterPixels <= px; px--)
				{
					for (var py = y + height; py < y + height + filterPixels; py++)
					{
						var pixel = GetAddPixelColor(px + 1, py - 1);
						
						texture.SetPixel(px, py, pixel);
					}
				}
			}

			// TopRight.
			{
				for (var px = x + width; px < x + width + filterPixels; px++)
				{
					for (var py = y + height; py < y + height + filterPixels; py++)
					{
						var pixel = GetAddPixelColor(px - 1, py - 1);

						texture.SetPixel(px, py, pixel);
					}
				}
			}
			
			// BottomLeft.
			{
				for (var px = x - 1; x - filterPixels <= px; px--)
				{
					for (var py = y - 1; y - filterPixels <= py; py--)
					{
						var pixel = GetAddPixelColor(px + 1, py + 1);

						texture.SetPixel(px, py, pixel);
					}
				}
			}

			// BottomRight.
			{
				for (var px = x + width; px < x + width + filterPixels; px++)
				{
					for (var py = y - 1; y - filterPixels <= py; py--)
					{
						var pixel = GetAddPixelColor(px - 1, py + 1);

						texture.SetPixel(px, py, pixel);
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
            var blockId = GetHashId(colorHash, colors);

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

        private ushort GetHashId(string hash, Color32[] colors)
        {
            // 同一ハッシュのエントリ一覧からピクセル一致するものを探す(ハッシュ衝突対策).
            if (blockIdByHashCode.TryGetValue(hash, out var entries))
            {
                foreach (var entry in entries)
                {
                    if (ColorsEqual(entry.colors, colors))
                    {
                        return entry.blockId;
                    }
                }
            }
            else
            {
                entries = new List<(ushort, Color32[])>();
                blockIdByHashCode.Add(hash, entries);
            }

            if (blockId == ushort.MaxValue)
            {
                Debug.LogError($"BlockId overflow. Max = {ushort.MaxValue}. Cannot add more unique blocks.");
            }

            var newId = blockId;

            entries.Add((newId, colors));

            blockId++;

            return newId;
        }

        private static bool ColorsEqual(Color32[] a, Color32[] b)
        {
            if (a == null || b == null){ return false; }
            if (a.Length != b.Length){ return false; }

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i].r != b[i].r || a[i].g != b[i].g || a[i].b != b[i].b || a[i].a != b[i].a)
                {
                    return false;
                }
            }

            return true;
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
                    var hasAlpha = 0x01 <= colors[y * width + x].a ? 0x01 : 0x00;

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

            // width * height が 8 の倍数でない場合、余りビットを書き込む.
            if (bit != 0)
            {
                alphaMap.Add(alphaBit);
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
