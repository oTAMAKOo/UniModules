
using System.Collections.Generic;

namespace Modules.Devkit.Markdown
{
    public enum MarkdownBlockType
    {
        Heading,
        Paragraph,
        CodeBlock,
        Quote,
        List,
        Table,
        HorizontalRule,
        Image,
    }

    public enum MarkdownTableAlign
    {
        Left,
        Center,
        Right,
    }

    /// <summary> Markdownのブロック要素 </summary>
    public sealed class MarkdownBlock
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public MarkdownBlockType Type { get; set; }

        /// <summary> 見出しの階層(1から6) </summary>
        public int Level { get; set; }

        /// <summary> 見出し・段落・コードの本文、画像の代替テキスト </summary>
        public string Text { get; set; }

        /// <summary> コードブロックの言語名 </summary>
        public string Language { get; set; }

        /// <summary> 画像のパス </summary>
        public string Url { get; set; }

        /// <summary> 番号付きリストか </summary>
        public bool Ordered { get; set; }

        /// <summary> 番号付きリストの開始番号 </summary>
        public int StartNumber { get; set; }

        /// <summary> 引用の中身 </summary>
        public List<MarkdownBlock> Children { get; set; }

        /// <summary> リストの項目毎の中身 </summary>
        public List<List<MarkdownBlock>> Items { get; set; }

        /// <summary> 表の見出し行 </summary>
        public string[] Header { get; set; }

        /// <summary> 表の列毎の揃え </summary>
        public MarkdownTableAlign[] Aligns { get; set; }

        /// <summary> 表のデータ行 </summary>
        public List<string[]> Rows { get; set; }

        //----- method -----
    }
}
