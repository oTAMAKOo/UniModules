
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
using Extensions;

namespace Modules.Devkit.Markdown
{
    /// <summary>
    /// Markdownを整形して表示するVisualElement.
    /// 見た目はスタイルシートで指定する. 各要素には md- で始まるクラス名が付く.
    /// </summary>
    public sealed class MarkdownView : VisualElement
    {
        //----- params -----

        public const string RootClassName = "md-root";
        public const string DarkClassName = "md-dark";
        public const string LightClassName = "md-light";

        public const string HeadingClassName = "md-heading";
        public const string ParagraphClassName = "md-paragraph";
        public const string LeadClassName = "md-lead";
        public const string CodeBlockClassName = "md-code-block";
        public const string CodeLanguageClassName = "md-code-lang";
        public const string CodeClassName = "md-code";
        public const string QuoteClassName = "md-quote";
        public const string ListClassName = "md-list";
        public const string ListItemClassName = "md-list-item";
        public const string ListMarkerClassName = "md-list-marker";
        public const string ListContentClassName = "md-list-content";
        public const string TableClassName = "md-table";
        public const string TableRowClassName = "md-table-row";
        public const string TableHeaderClassName = "md-table-header";
        public const string TableRowAltClassName = "md-table-row-alt";
        public const string TableCellClassName = "md-table-cell";
        public const string HorizontalRuleClassName = "md-hr";
        public const string ImageClassName = "md-image";
        public const string FirstClassName = "md-first";

        private const string CodeFontPath = "Fonts/RobotoMono/RobotoMono-Regular.ttf";

        private const int MinColumnWeight = 6;
        private const int MaxColumnWeight = 40;

        private static readonly string[] BulletMarkers = new string[] { "•", "◦", "▪" };

        private static readonly CustomStyleProperty<Color> LinkColorProperty = new CustomStyleProperty<Color>("--md-link-color");
        private static readonly CustomStyleProperty<Color> CodeColorProperty = new CustomStyleProperty<Color>("--md-inline-code-color");
        private static readonly CustomStyleProperty<Color> CodeBackgroundColorProperty = new CustomStyleProperty<Color>("--md-inline-code-background-color");
        private static readonly CustomStyleProperty<float> LineHeightProperty = new CustomStyleProperty<float>("--md-line-height");

        //----- field -----

        private string markdown = null;
        private string assetPath = null;

        private List<MarkdownBlock> blocks = null;

        private MarkdownInlineStyle inlineStyle = null;

        private float lineHeight = 0f;

        private Font codeFont = null;

        //----- property -----

        //----- method -----

        public MarkdownView()
        {
            var isDark = EditorGUIUtility.isProSkin;

            AddToClassList(RootClassName);
            AddToClassList(isDark ? DarkClassName : LightClassName);

            inlineStyle = new MarkdownInlineStyle()
            {
                LinkColor = isDark ? new Color(0.45f, 0.68f, 1f) : new Color(0.04f, 0.4f, 0.84f),
                CodeColor = isDark ? new Color(0.9f, 0.62f, 0.45f) : new Color(0.7f, 0.25f, 0.1f),
                CodeBackgroundColor = isDark ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.06f),
            };

            codeFont = EditorGUIUtility.Load(CodeFontPath) as Font;

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        /// <summary> スタイルシートを差し替える </summary>
        public void SetStyleSheet(StyleSheet styleSheet)
        {
            styleSheets.Clear();

            if (styleSheet != null)
            {
                styleSheets.Add(styleSheet);
            }
        }

        /// <summary> 表示する内容を設定する </summary>
        /// <param name="markdown"> Markdownのテキスト </param>
        /// <param name="assetPath"> 相対パスのリンク・画像の基準になるアセットパス </param>
        public void SetMarkdown(string markdown, string assetPath)
        {
            this.markdown = markdown;
            this.assetPath = assetPath;

            blocks = MarkdownParser.Parse(markdown);

            Rebuild();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            var style = new MarkdownInlineStyle()
            {
                LinkColor = inlineStyle.LinkColor,
                CodeColor = inlineStyle.CodeColor,
                CodeBackgroundColor = inlineStyle.CodeBackgroundColor,
            };

            var height = lineHeight;

            if (e.customStyle.TryGetValue(LinkColorProperty, out var linkColor))
            {
                style.LinkColor = linkColor;
            }

            if (e.customStyle.TryGetValue(CodeColorProperty, out var codeColor))
            {
                style.CodeColor = codeColor;
            }

            if (e.customStyle.TryGetValue(CodeBackgroundColorProperty, out var codeBackgroundColor))
            {
                style.CodeBackgroundColor = codeBackgroundColor;
            }

            if (e.customStyle.TryGetValue(LineHeightProperty, out var lineHeightValue))
            {
                height = lineHeightValue;
            }

            if (style.IsSame(inlineStyle) && Mathf.Approximately(height, lineHeight)){ return; }

            inlineStyle = style;
            lineHeight = height;

            // スタイルの解決中は階層を変更できないため、次の更新で作り直す.
            schedule.Execute(Rebuild);
        }

        private void Rebuild()
        {
            Clear();

            if (blocks == null){ return; }

            AddBlocks(this, blocks, 0);

            // 先頭の大見出しの直後にある段落は、リード文として表示する.

            if (2 <= blocks.Count && childCount == blocks.Count)
            {
                var isTitle = blocks[0].Type == MarkdownBlockType.Heading && blocks[0].Level == 1;

                if (isTitle && blocks[1].Type == MarkdownBlockType.Paragraph)
                {
                    this[1].AddToClassList(LeadClassName);
                }
            }
        }

        private void AddBlocks(VisualElement parent, List<MarkdownBlock> targets, int listDepth)
        {
            for (var i = 0; i < targets.Count; i++)
            {
                var element = CreateBlock(targets[i], listDepth);

                if (element == null){ continue; }

                if (i == 0)
                {
                    element.AddToClassList(FirstClassName);
                }

                parent.Add(element);
            }
        }

        private VisualElement CreateBlock(MarkdownBlock block, int listDepth)
        {
            switch (block.Type)
            {
                case MarkdownBlockType.Heading:
                    {
                        var label = CreateTextLabel(block.Text, false);

                        label.AddToClassList(HeadingClassName);
                        label.AddToClassList($"md-h{block.Level}");

                        return label;
                    }

                case MarkdownBlockType.Paragraph:
                    {
                        var label = CreateTextLabel(block.Text, true);

                        label.AddToClassList(ParagraphClassName);

                        return label;
                    }

                case MarkdownBlockType.CodeBlock:
                    return CreateCodeBlock(block);

                case MarkdownBlockType.Quote:
                    {
                        var quote = new VisualElement();

                        quote.AddToClassList(QuoteClassName);

                        AddBlocks(quote, block.Children, listDepth);

                        return quote;
                    }

                case MarkdownBlockType.List:
                    return CreateList(block, listDepth);

                case MarkdownBlockType.Table:
                    return CreateTable(block);

                case MarkdownBlockType.HorizontalRule:
                    {
                        var rule = new VisualElement();

                        rule.AddToClassList(HorizontalRuleClassName);

                        return rule;
                    }

                case MarkdownBlockType.Image:
                    return CreateImage(block);
            }

            return null;
        }

        private VisualElement CreateCodeBlock(MarkdownBlock block)
        {
            var container = new VisualElement();

            container.AddToClassList(CodeBlockClassName);

            if (!string.IsNullOrEmpty(block.Language))
            {
                var language = new Label(block.Language);

                language.enableRichText = false;
                language.AddToClassList(CodeLanguageClassName);

                container.Add(language);
            }

            var code = new Label(block.Text);

            code.enableRichText = false;
            code.selection.isSelectable = true;
            code.AddToClassList(CodeClassName);

            if (codeFont != null)
            {
                code.style.unityFontDefinition = FontDefinition.FromFont(codeFont);
            }

            container.Add(code);

            return container;
        }

        private VisualElement CreateList(MarkdownBlock block, int listDepth)
        {
            var list = new VisualElement();

            list.AddToClassList(ListClassName);

            for (var i = 0; i < block.Items.Count; i++)
            {
                var item = new VisualElement();

                item.AddToClassList(ListItemClassName);

                var markerText = block.Ordered ? $"{block.StartNumber + i}." : BulletMarkers[Mathf.Min(listDepth, BulletMarkers.Length - 1)];

                var marker = new Label(markerText);

                marker.enableRichText = false;
                marker.AddToClassList(ListMarkerClassName);

                var content = new VisualElement();

                content.AddToClassList(ListContentClassName);

                AddBlocks(content, block.Items[i], listDepth + 1);

                item.Add(marker);
                item.Add(content);

                list.Add(item);
            }

            return list;
        }

        private VisualElement CreateTable(MarkdownBlock block)
        {
            var table = new VisualElement();

            table.AddToClassList(TableClassName);

            // 列の幅は、各列で最も長いセルの文字数の比で配分する.

            var weights = new int[block.Header.Length];

            for (var i = 0; i < weights.Length; i++)
            {
                var length = MarkdownInline.GetDisplayLength(block.Header[i]);

                foreach (var row in block.Rows)
                {
                    length = Mathf.Max(length, MarkdownInline.GetDisplayLength(row[i]));
                }

                weights[i] = Mathf.Clamp(length, MinColumnWeight, MaxColumnWeight);
            }

            table.Add(CreateTableRow(block.Header, block.Aligns, weights, true, false));

            for (var i = 0; i < block.Rows.Count; i++)
            {
                table.Add(CreateTableRow(block.Rows[i], block.Aligns, weights, false, i % 2 == 1));
            }

            return table;
        }

        private VisualElement CreateTableRow(string[] cells, MarkdownTableAlign[] aligns, int[] weights, bool isHeader, bool isAlt)
        {
            var row = new VisualElement();

            row.AddToClassList(TableRowClassName);

            if (isHeader)
            {
                row.AddToClassList(TableHeaderClassName);
            }

            if (isAlt)
            {
                row.AddToClassList(TableRowAltClassName);
            }

            for (var i = 0; i < cells.Length; i++)
            {
                var cell = CreateTextLabel(cells[i], !isHeader);

                cell.AddToClassList(TableCellClassName);

                if (i == 0)
                {
                    cell.AddToClassList(FirstClassName);
                }

                cell.style.flexGrow = weights[i];
                cell.style.flexShrink = 1;
                cell.style.flexBasis = 0;

                switch (aligns[i])
                {
                    case MarkdownTableAlign.Center:
                        cell.style.unityTextAlign = TextAnchor.UpperCenter;
                        break;

                    case MarkdownTableAlign.Right:
                        cell.style.unityTextAlign = TextAnchor.UpperRight;
                        break;
                }

                row.Add(cell);
            }

            return row;
        }

        private VisualElement CreateImage(MarkdownBlock block)
        {
            var texture = LoadTexture(block.Url);

            // 読み込めない画像は代替テキストを表示する.
            if (texture == null)
            {
                var label = CreateTextLabel(block.Text, false);

                label.AddToClassList(ParagraphClassName);

                return label;
            }

            var image = new Image();

            image.image = texture;
            image.scaleMode = ScaleMode.ScaleToFit;
            image.style.maxWidth = texture.width;
            image.AddToClassList(ImageClassName);

            return image;
        }

        /// <summary> インライン記法を解釈したラベルを生成する </summary>
        private Label CreateTextLabel(string text, bool applyLineHeight)
        {
            var links = new List<string>();

            var richText = MarkdownInline.ToRichText(text, inlineStyle, links);

            if (applyLineHeight && 0f < lineHeight)
            {
                richText = $"<line-height={lineHeight}%>{richText}";
            }

            var label = new Label(richText);

            label.enableRichText = true;

            if (0 < links.Count)
            {
                label.RegisterCallback<PointerUpLinkTagEvent>(e =>
                {
                    if (int.TryParse(e.linkID, out var linkIndex) && 0 <= linkIndex && linkIndex < links.Count)
                    {
                        OpenLink(links[linkIndex]);
                    }
                });
            }

            return label;
        }

        private void OpenLink(string url)
        {
            if (string.IsNullOrEmpty(url)){ return; }

            if (url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith("mailto:"))
            {
                Application.OpenURL(url);
                return;
            }

            var fullPath = ResolvePath(url);

            if (string.IsNullOrEmpty(fullPath)){ return; }

            // アセットとして読み込めるファイルは選択状態にする.

            var linkAssetPath = UnityPathUtility.ConvertFullPathToAssetPath(fullPath);

            if (!string.IsNullOrEmpty(linkAssetPath))
            {
                var asset = AssetDatabase.LoadMainAssetAtPath(linkAssetPath);

                if (asset != null)
                {
                    Selection.activeObject = asset;
                    EditorGUIUtility.PingObject(asset);
                    return;
                }
            }

            if (File.Exists(fullPath) || Directory.Exists(fullPath))
            {
                EditorUtility.OpenWithDefaultApp(fullPath);
            }
        }

        private Texture LoadTexture(string url)
        {
            var fullPath = ResolvePath(url);

            if (string.IsNullOrEmpty(fullPath)){ return null; }

            var textureAssetPath = UnityPathUtility.ConvertFullPathToAssetPath(fullPath);

            if (string.IsNullOrEmpty(textureAssetPath)){ return null; }

            return AssetDatabase.LoadAssetAtPath<Texture>(textureAssetPath);
        }

        /// <summary> アセットからの相対パスをフルパスに変換する </summary>
        private string ResolvePath(string url)
        {
            if (string.IsNullOrEmpty(assetPath)){ return null; }

            // アンカーを取り除く.

            var anchorIndex = url.IndexOf('#');

            if (0 <= anchorIndex)
            {
                url = url.Substring(0, anchorIndex);
            }

            if (string.IsNullOrEmpty(url)){ return null; }

            url = Uri.UnescapeDataString(url);

            var assetFullPath = Path.GetFullPath(assetPath);

            var directory = Path.GetDirectoryName(assetFullPath);

            return PathUtility.ConvertPathSeparator(Path.GetFullPath(Path.Combine(directory, url)));
        }
    }
}
