
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Extensions;
using Modules.Devkit.TextureEdit;

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

        public PatternTextureData Generate(string exportPath, int blockSize, int padding, Texture2D[] sourceTextures, bool hasAlphaMap)
        {
            var patternTextureData = new PatternTextureData();

            blockId = 0;
            blockIdbyHashCode = new Dictionary<string, ushort?>();

            foreach (var sourceTexture in sourceTextures)
            {
                var editableTexture = new EditableTexture(sourceTexture);

                editableTexture.Editable();
            }

            var patternTargetDatas = ReadTextureBlock(blockSize, sourceTextures);

            // 補間用ピクセル分のパディングを追加.
            padding += 2;

            // 透明ピクセル、同じピクセル情報のブロックは対象外.
            var totalBlockCount = patternTargetDatas
                .SelectMany(x => x.blocks)
                .Select(x => x.blockId)
                .Distinct()
                .Count();

            var directory = Path.GetDirectoryName(exportPath);
            var textureName = Path.ChangeExtension(Path.GetFileName(exportPath), ".png");
            var texturePath = PathUtility.Combine(directory, textureName);

            var textureSize = CalcRequireTextureSize(blockSize, padding, totalBlockCount);

            var texture = TextureUtility.CreateEmptyTexture(textureSize, textureSize);

            patternTextureData.PatternData = BuildPatternData(patternTargetDatas);

            patternTextureData.PatternBlocks = BitBlockTransfer(texture, blockSize, padding, patternTargetDatas, hasAlphaMap);

            File.WriteAllBytes(texturePath, texture.EncodeToPNG());

            AssetDatabase.ImportAsset(texturePath);

            patternTextureData.Texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            return patternTextureData;
        }

        // テクスチャを読み込み.
        private PatternTargetData[] ReadTextureBlock(int blockSize, Texture2D[] textures)
        {
            var list = new List<PatternTargetData>();

            foreach (var texture in textures)
            {
                var blockList = new List<TextureBlock>();

                var bx = texture.width / blockSize;
                var by = texture.height / blockSize;

                var pixels = texture.GetPixels32();

                // 残りの領域があった場合追加.
                bx += texture.width % blockSize != 0 ? 1 : 0;
                by += texture.height % blockSize != 0 ? 1 : 0;

                for (var y = 0; y < by; y++)
                {
                    for (var x = 0; x < bx; x++)
                    {
                        var block = GetTextureBlock(texture, x, y, blockSize, pixels);

                        blockList.Add(block);
                    }
                }

                list.Add(new PatternTargetData() { texture = texture, bx = bx, by = by, blocks = blockList.ToArray() });
            }

            return list.ToArray();
        }

        // 2のべき乗で全ブロックを格納できるテクスチャサイズを計算.
        private int CalcRequireTextureSize(int blockSize, int padding, int totalBlockCount)
        {
            var textureSize = 2;

            // パディングの分も含める.
            var totalBlockSize = blockSize + padding;

            while (true)
            {
                textureSize *= 2;

                // 一列に入るブロック数.
                var count = (float)Math.Floor((float)(textureSize - padding) / totalBlockSize);

                if (totalBlockCount < count * count) { break; }
            }

            return textureSize;
        }

        // 元になったテクスチャ情報構築.
        private PatternData[] BuildPatternData(PatternTargetData[] targetDatas)
        {
            var result = new List<PatternData>();

            foreach (var targetData in targetDatas)
            {
                var texture = targetData.texture;
                var assetPath = AssetDatabase.GetAssetPath(texture);
                var fullPath = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);
                
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
        private PatternBlockData[] BitBlockTransfer(Texture2D texture, int blockSize, int padding, PatternTargetData[] patternTargetDatas, bool hasAlphaMap)
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
                    if (!blockDataDictionary.ContainsKey(item.blockId))
                    {
                        if (texture.width < transX + item.width + padding)
                        {
                            transX = x_start;
                            transY += blockSize + padding;
                        }

                        texture.SetPixels32(transX, transY, item.width, item.height, item.colors);

                        AddCompletionPixels(texture, transX, transY, item.width, item.height);

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
                        };

                        blockDataDictionary.Add(item.blockId, blockData);

                        // 次の位置.
                        transX += item.width + padding;
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            texture.Apply(true, false);

            return blockDataDictionary.Values.ToArray();
        }

        // 補完時に周りのピクセル情報を巻き込まないように外周にピクセルを追加.
        private void AddCompletionPixels(Texture2D texture, int x, int y, int width, int height)
        {
            var pixel = Color.clear;

            // Top / Bottom.
            for (var px = x - 1; px < x + width + 1; px++)
            {
                // Top.
                pixel = texture.GetPixel(px, y);
                //texture.SetPixel(px, y, Color.magenta);
                texture.SetPixel(px, y - 1, pixel);

                // Bottom.
                pixel = texture.GetPixel(px, y + height - 1);
                //texture.SetPixel(px, y + height - 1, Color.magenta);
                texture.SetPixel(px, y + height, pixel);
            }

            // Left / Right.
            for (var py = y - 1; py < y + height + 1; py++)
            {
                // Left.
                pixel = texture.GetPixel(x, py);
                //texture.SetPixel(x, py, Color.magenta);
                texture.SetPixel(x - 1, py, pixel);

                // Right.
                pixel = texture.GetPixel(x + width - 1, py);
                //texture.SetPixel(x + width - 1, py, Color.magenta);
                texture.SetPixel(x + width, py, pixel);
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
