
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using UnityEditor;

namespace Modules.Devkit.TextureViewer
{
    public sealed class CompressInfoView : InfoView<CompressInfoView.CompressColumn>
    {
        //----- params -----

        public const string TextureFormatLabel = "Format";

        public enum CompressColumn
        {
            Warning,
            TextureName,
            TextureSize,
            Overridden,
            Format,
            MaxSize,
            FileSize,
        }

        public static readonly Dictionary<CompressColumn, ColumnInfo> ColumnInfos = new Dictionary<CompressColumn, ColumnInfo>()
        {
            { CompressColumn.Warning, new ColumnInfo(string.Empty, 24) },
            { CompressColumn.TextureName, new ColumnInfo(InfoTreeView.TextureNameLabel) },
            { CompressColumn.TextureSize, new ColumnInfo("Size", 90f) },
            { CompressColumn.Overridden, new ColumnInfo("Overridden", 80f) },
            { CompressColumn.Format, new ColumnInfo(TextureFormatLabel, 150f) },
            { CompressColumn.MaxSize, new ColumnInfo("MaxSize", 70f) },
            { CompressColumn.FileSize, new ColumnInfo("FileSize", 80f) },
        };

        //----- field -----

        //----- property -----

        public BuildTargetGroup Platform { get; set; }

        protected override CompressColumn WarningColumn { get { return CompressColumn.Warning; } }

        protected override CompressColumn TextureNameColumn { get { return CompressColumn.TextureName; } }

        //----- method -----

        protected override object GetValue(CompressColumn column, TextureInfo textureInfo)
        {
            switch (column)
            {
                case CompressColumn.Warning:
                    return null;
                case CompressColumn.TextureName:
                    return textureInfo.TextureName;
                case CompressColumn.TextureSize:
                    return textureInfo.GetTextureSizeText();
                case CompressColumn.Overridden:
                    return textureInfo.GetCompressOverridden(Platform);
                case CompressColumn.Format:
                    return textureInfo.GetFormatText(Platform);
                case CompressColumn.MaxSize:
                    return textureInfo.GetMaxTextureSize(Platform).ToString();
                case CompressColumn.FileSize:
                    return textureInfo.GetFileSizeText();
                default:
                    return "---";
            }
        }

        public TextureInfo[] Sort(int columnIndex, bool ascending, TextureInfo[] infos)
        {
            var columns = Enum.GetValues(typeof(CompressColumn)).Cast<CompressColumn>().ToArray();

            var column = columns.ElementAt(columnIndex);

            IOrderedEnumerable<TextureInfo> orderedInfos = null;

            switch (column)
            {
                case CompressColumn.Warning:
                    orderedInfos = infos.Order(ascending, x => x.HasWarning);
                    break;
                case CompressColumn.TextureName:
                    orderedInfos = infos.Order(ascending, x => x.TextureName, new NaturalComparer());
                    break;
                case CompressColumn.TextureSize:
                    orderedInfos = infos.Order(ascending, x => x.TextureSize);
                    break;
                case CompressColumn.Overridden:
                    orderedInfos = infos.Order(ascending, x => x.GetCompressOverridden(Platform));
                    break;
                case CompressColumn.Format:
                    orderedInfos = infos.Order(ascending, x => x.GetFormatText(Platform));
                    break;
                case CompressColumn.MaxSize:
                    orderedInfos = infos.Order(ascending, x => x.GetMaxTextureSize(Platform));
                    break;
                case CompressColumn.FileSize:
                    orderedInfos = infos.Order(ascending, x => x.FileSize);
                    break;
            }

            if (orderedInfos == null){ return infos; }

            return orderedInfos.ThenBy(x => x.AssetPath, new NaturalComparer()).ToArray();
        }
    }
}