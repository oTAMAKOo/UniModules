
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;

namespace Modules.Devkit.TextureViewer
{
    public sealed class TextureInfoView : InfoView<TextureInfoView.TextureColumn>
    {
        //----- params -----

        public enum TextureColumn
        {
            Warning,
            TextureName,
            TextureSize,
            TextureType,
            AlphaIsTransparency,
            ReadWrite,
            GenerateMipMap,
            NonPowerOfTwo,
            FileSize,
        }

        public static readonly Dictionary<TextureColumn, ColumnInfo> ColumnInfos = new Dictionary<TextureColumn, ColumnInfo>()
        {
            { TextureColumn.Warning, new ColumnInfo(string.Empty, 24) },
            { TextureColumn.TextureName, new ColumnInfo(InfoTreeView.TextureNameLabel){ Alignment = TextAlignment.Left } },
            { TextureColumn.TextureSize, new ColumnInfo("Size", 90f) },
            { TextureColumn.TextureType, new ColumnInfo("TextureType", 80f) },
            { TextureColumn.AlphaIsTransparency, new ColumnInfo("Transparency", 90f) },
            { TextureColumn.ReadWrite, new ColumnInfo("ReadWrite", 70f) },
            { TextureColumn.GenerateMipMap, new ColumnInfo("MipMap", 70f) },
            { TextureColumn.NonPowerOfTwo, new ColumnInfo("NPOT", 70f) },
            { TextureColumn.FileSize, new ColumnInfo("FileSize", 80f) },
        };

        //----- field -----

        //----- property -----

        protected override TextureColumn WarningColumn { get { return TextureColumn.Warning; } }

        protected override TextureColumn TextureNameColumn { get { return TextureColumn.TextureName; } }

        //----- method -----

        protected override object GetValue(TextureColumn column, TextureInfo textureInfo)
        {
            switch (column)
            {
                case TextureColumn.Warning:
                    return null;
                case TextureColumn.TextureName:
                    return textureInfo.GetTextureName();
                case TextureColumn.TextureType:
                    return textureInfo.TextureImporter.textureType.ToString();
                case TextureColumn.NonPowerOfTwo:
                    return textureInfo.TextureImporter.npotScale.ToString();
                case TextureColumn.GenerateMipMap:
                    return textureInfo.TextureImporter.mipmapEnabled;
                case TextureColumn.AlphaIsTransparency:
                    return textureInfo.TextureImporter.alphaIsTransparency;
                case TextureColumn.ReadWrite:
                    return textureInfo.TextureImporter.isReadable;
                case TextureColumn.TextureSize:
                    return textureInfo.GetTextureSizeText();
                case TextureColumn.FileSize:
                    return textureInfo.GetFileSizeText();
                default:
                    return "---";
            }
        }

        public TextureInfo[] Sort(int columnIndex, bool ascending, TextureInfo[] infos)
        {
            var columns = Enum.GetValues(typeof(TextureColumn)).Cast<TextureColumn>().ToArray();

            var column = columns.ElementAtOrDefault(columnIndex, TextureColumn.TextureName);

            IOrderedEnumerable<TextureInfo> orderedInfos = null;

            switch (column)
            {
                case TextureColumn.Warning:
                    orderedInfos = infos.Order(ascending, x => x.HasWarning());
                    break;
                case TextureColumn.TextureName:
                    orderedInfos = infos.Order(ascending, x => x.GetTextureName(), new NaturalComparer());
                    break;
                case TextureColumn.TextureSize:
                    orderedInfos = infos.Order(ascending, x => x.GetTextureSize());
                    break;
                case TextureColumn.TextureType:
                    orderedInfos = infos.Order(ascending, x => x.TextureImporter.textureType.ToString());
                    break;
                case TextureColumn.NonPowerOfTwo:
                    orderedInfos = infos.Order(ascending, x => x.TextureImporter.npotScale.ToString());
                    break;
                case TextureColumn.GenerateMipMap:
                    orderedInfos = infos.Order(ascending, x => x.TextureImporter.mipmapEnabled);
                    break;
                case TextureColumn.AlphaIsTransparency:
                    orderedInfos = infos.Order(ascending, x => x.TextureImporter.alphaIsTransparency);
                    break;
                case TextureColumn.ReadWrite:
                    orderedInfos = infos.Order(ascending, x => x.TextureImporter.isReadable);
                    break;
                case TextureColumn.FileSize:
                    orderedInfos = infos.Order(ascending, x => x.GetFileSize());
                    break;
            }

            if (orderedInfos == null){ return infos; }

            return orderedInfos.ThenBy(x => x.AssetPath, new NaturalComparer()).ToArray();
        }
    }
}