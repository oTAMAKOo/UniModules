
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Modules.Devkit.Markdown
{
    /// <summary> Markdownのテキストをブロック要素に分解する </summary>
    public static class MarkdownParser
    {
        //----- params -----

        private const int TabWidth = 4;

        private static readonly Regex HeadingRegex = new Regex(@"^ {0,3}(#{1,6})[ \t]+(.*?)(?:[ \t]+#+)?[ \t]*$", RegexOptions.Compiled);

        private static readonly Regex HorizontalRuleRegex = new Regex(@"^ {0,3}([-*_])(?:[ \t]*\1){2,}[ \t]*$", RegexOptions.Compiled);

        private static readonly Regex ListMarkerRegex = new Regex(@"^([ \t]*)([-*+]|\d{1,9}[.)])([ \t]+)(.*)$", RegexOptions.Compiled);

        private static readonly Regex FenceRegex = new Regex(@"^([ \t]*)(`{3,}|~{3,})[ \t]*([^`]*)$", RegexOptions.Compiled);

        private static readonly Regex QuoteRegex = new Regex(@"^ {0,3}>[ ]?(.*)$", RegexOptions.Compiled);

        private static readonly Regex TableSeparatorRegex = new Regex(@"^[ \t]*\|?[ \t]*:?-+:?[ \t]*(?:\|[ \t]*:?-+:?[ \t]*)*\|?[ \t]*$", RegexOptions.Compiled);

        private static readonly Regex ImageLineRegex = new Regex(@"^[ \t]*!\[([^\]]*)\]\(([^)\s]+)(?:\s+""[^""]*"")?\)[ \t]*$", RegexOptions.Compiled);

        private static readonly Regex TaskRegex = new Regex(@"^\[([ xX])\][ \t]+", RegexOptions.Compiled);

        //----- field -----

        //----- property -----

        //----- method -----

        public static List<MarkdownBlock> Parse(string markdown)
        {
            if (string.IsNullOrEmpty(markdown)){ return new List<MarkdownBlock>(); }

            var text = markdown.Replace("\r\n", "\n").Replace('\r', '\n').TrimStart('﻿');

            var lines = text.Split('\n').ToList();

            return ParseBlocks(lines);
        }

        private static List<MarkdownBlock> ParseBlocks(List<string> lines)
        {
            var blocks = new List<MarkdownBlock>();

            var index = 0;

            while (index < lines.Count)
            {
                var line = lines[index];

                if (IsBlank(line))
                {
                    index++;
                    continue;
                }

                // コードブロック.

                var fence = FenceRegex.Match(line);

                if (fence.Success)
                {
                    blocks.Add(ParseCodeBlock(lines, ref index, fence));
                    continue;
                }

                // 見出し.

                var heading = HeadingRegex.Match(line);

                if (heading.Success)
                {
                    var block = new MarkdownBlock()
                    {
                        Type = MarkdownBlockType.Heading,
                        Level = heading.Groups[1].Value.Length,
                        Text = heading.Groups[2].Value.Trim(),
                    };

                    blocks.Add(block);
                    index++;
                    continue;
                }

                // 区切り線.

                if (HorizontalRuleRegex.IsMatch(line))
                {
                    blocks.Add(new MarkdownBlock() { Type = MarkdownBlockType.HorizontalRule });
                    index++;
                    continue;
                }

                // 引用.

                if (QuoteRegex.IsMatch(line))
                {
                    blocks.Add(ParseQuote(lines, ref index));
                    continue;
                }

                // 表.

                if (IsTableStart(lines, index))
                {
                    blocks.Add(ParseTable(lines, ref index));
                    continue;
                }

                // 画像.

                var image = ImageLineRegex.Match(line);

                if (image.Success)
                {
                    var block = new MarkdownBlock()
                    {
                        Type = MarkdownBlockType.Image,
                        Text = image.Groups[1].Value,
                        Url = image.Groups[2].Value,
                    };

                    blocks.Add(block);
                    index++;
                    continue;
                }

                // リスト.

                if (ListMarkerRegex.IsMatch(line))
                {
                    blocks.Add(ParseList(lines, ref index));
                    continue;
                }

                // 段落.

                blocks.Add(ParseParagraph(lines, ref index));
            }

            return blocks;
        }

        private static MarkdownBlock ParseCodeBlock(List<string> lines, ref int index, Match fence)
        {
            var indent = GetIndentWidth(fence.Groups[1].Value);
            var marker = fence.Groups[2].Value;
            var language = fence.Groups[3].Value.Trim();

            var builder = new StringBuilder();

            index++;

            while (index < lines.Count)
            {
                var line = lines[index];

                index++;

                var trimmed = line.Trim();

                // 開始と同じ記号で、同じ長さ以上なら終了.
                if (trimmed.StartsWith(marker) && trimmed.Trim(marker[0]).Length == 0){ break; }

                if (0 < builder.Length)
                {
                    builder.Append('\n');
                }

                builder.Append(RemoveIndent(line, indent).Replace("\t", new string(' ', TabWidth)));
            }

            var block = new MarkdownBlock()
            {
                Type = MarkdownBlockType.CodeBlock,
                Language = language,
                Text = builder.ToString(),
            };

            return block;
        }

        private static MarkdownBlock ParseQuote(List<string> lines, ref int index)
        {
            var quoteLines = new List<string>();

            while (index < lines.Count)
            {
                var line = lines[index];

                var match = QuoteRegex.Match(line);

                if (match.Success)
                {
                    quoteLines.Add(match.Groups[1].Value);
                    index++;
                    continue;
                }

                // 記号の無い行は、直前が段落の続きのときだけ引用に含める.
                if (IsBlank(line) || IsBlockStart(lines, index)){ break; }

                if (quoteLines.Count == 0 || IsBlank(quoteLines[quoteLines.Count - 1])){ break; }

                quoteLines.Add(line);
                index++;
            }

            var block = new MarkdownBlock()
            {
                Type = MarkdownBlockType.Quote,
                Children = ParseBlocks(quoteLines),
            };

            return block;
        }

        private static MarkdownBlock ParseTable(List<string> lines, ref int index)
        {
            var header = SplitTableRow(lines[index]);
            var separator = SplitTableRow(lines[index + 1]);

            var aligns = new MarkdownTableAlign[header.Length];

            for (var i = 0; i < header.Length; i++)
            {
                var cell = i < separator.Length ? separator[i].Trim() : string.Empty;

                var left = cell.StartsWith(":");
                var right = cell.EndsWith(":");

                if (left && right)
                {
                    aligns[i] = MarkdownTableAlign.Center;
                }
                else if (right)
                {
                    aligns[i] = MarkdownTableAlign.Right;
                }
                else
                {
                    aligns[i] = MarkdownTableAlign.Left;
                }
            }

            index += 2;

            var rows = new List<string[]>();

            while (index < lines.Count)
            {
                var line = lines[index];

                if (IsBlank(line) || !line.Contains("|")){ break; }

                var cells = SplitTableRow(line);

                // 列数を見出し行に合わせる.

                var row = new string[header.Length];

                for (var i = 0; i < row.Length; i++)
                {
                    row[i] = i < cells.Length ? cells[i] : string.Empty;
                }

                rows.Add(row);
                index++;
            }

            var block = new MarkdownBlock()
            {
                Type = MarkdownBlockType.Table,
                Header = header,
                Aligns = aligns,
                Rows = rows,
            };

            return block;
        }

        private static MarkdownBlock ParseList(List<string> lines, ref int index)
        {
            var first = ListMarkerRegex.Match(lines[index]);

            var baseIndent = GetIndentWidth(first.Groups[1].Value);
            var ordered = char.IsDigit(first.Groups[2].Value[0]);

            var startNumber = 1;

            if (ordered)
            {
                var number = first.Groups[2].Value;

                int.TryParse(number.Substring(0, number.Length - 1), out startNumber);
            }

            var items = new List<List<MarkdownBlock>>();

            while (index < lines.Count)
            {
                var match = ListMarkerRegex.Match(lines[index]);

                if (!match.Success){ break; }

                var marker = match.Groups[2].Value;

                if (GetIndentWidth(match.Groups[1].Value) != baseIndent){ break; }

                if (char.IsDigit(marker[0]) != ordered){ break; }

                // 項目の本文が始まる桁.

                var spaces = match.Groups[3].Value.Length;

                var contentIndent = baseIndent + marker.Length + (spaces <= TabWidth ? spaces : 1);

                var content = match.Groups[4].Value;

                // チェックボックス.

                var task = TaskRegex.Match(content);

                if (task.Success)
                {
                    var check = task.Groups[1].Value == " " ? "☐ " : "☑ ";

                    content = check + content.Substring(task.Length);
                }

                var itemLines = new List<string>() { content };

                index++;

                while (index < lines.Count)
                {
                    var line = lines[index];

                    if (IsBlank(line))
                    {
                        // 空行の次の行が字下げされていれば、項目の続きとして扱う.

                        var next = index + 1;

                        while (next < lines.Count && IsBlank(lines[next]))
                        {
                            next++;
                        }

                        if (lines.Count <= next){ break; }

                        if (GetIndentWidth(lines[next]) <= baseIndent){ break; }

                        itemLines.Add(string.Empty);
                        index++;
                        continue;
                    }

                    if (baseIndent < GetIndentWidth(line))
                    {
                        itemLines.Add(RemoveIndent(line, contentIndent));
                        index++;
                        continue;
                    }

                    // 字下げが同じか浅い行は、別のブロックの開始でなければ段落の続き.
                    if (IsBlockStart(lines, index)){ break; }

                    itemLines.Add(line.Trim());
                    index++;
                }

                items.Add(ParseBlocks(itemLines));

                // 空行を挟んで同じ階層の項目が続く場合は、同じリストとして扱う.

                var lookAhead = index;

                while (lookAhead < lines.Count && IsBlank(lines[lookAhead]))
                {
                    lookAhead++;
                }

                if (lookAhead != index && lookAhead < lines.Count)
                {
                    var nextMatch = ListMarkerRegex.Match(lines[lookAhead]);

                    if (nextMatch.Success && GetIndentWidth(nextMatch.Groups[1].Value) == baseIndent)
                    {
                        index = lookAhead;
                    }
                }
            }

            var block = new MarkdownBlock()
            {
                Type = MarkdownBlockType.List,
                Ordered = ordered,
                StartNumber = startNumber,
                Items = items,
            };

            return block;
        }

        private static MarkdownBlock ParseParagraph(List<string> lines, ref int index)
        {
            var builder = new StringBuilder();

            // 先頭行は他のブロックに当たらなかった行なので、無条件で段落に含める.

            builder.Append(lines[index].Trim());

            index++;

            while (index < lines.Count)
            {
                var line = lines[index];

                if (IsBlank(line) || IsBlockStart(lines, index)){ break; }

                builder.Append('\n');
                builder.Append(line.Trim());

                index++;
            }

            var block = new MarkdownBlock()
            {
                Type = MarkdownBlockType.Paragraph,
                Text = builder.ToString(),
            };

            return block;
        }

        /// <summary> 段落を中断して別のブロックが始まる行か </summary>
        private static bool IsBlockStart(List<string> lines, int index)
        {
            var line = lines[index];

            if (FenceRegex.IsMatch(line)){ return true; }

            if (HeadingRegex.IsMatch(line)){ return true; }

            if (HorizontalRuleRegex.IsMatch(line)){ return true; }

            if (QuoteRegex.IsMatch(line)){ return true; }

            if (ListMarkerRegex.IsMatch(line)){ return true; }

            if (IsTableStart(lines, index)){ return true; }

            return false;
        }

        private static bool IsTableStart(List<string> lines, int index)
        {
            if (lines.Count <= index + 1){ return false; }

            if (!lines[index].Contains("|")){ return false; }

            var separator = lines[index + 1];

            return separator.Contains("|") && TableSeparatorRegex.IsMatch(separator);
        }

        /// <summary> 表の1行をセルに分割する </summary>
        private static string[] SplitTableRow(string line)
        {
            var cells = new List<string>();

            var builder = new StringBuilder();

            var inCode = false;

            var text = line.Trim();

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '\\' && i + 1 < text.Length && text[i + 1] == '|')
                {
                    builder.Append('|');
                    i++;
                    continue;
                }

                if (c == '`')
                {
                    inCode = !inCode;
                }

                if (c == '|' && !inCode)
                {
                    cells.Add(builder.ToString().Trim());
                    builder.Length = 0;
                    continue;
                }

                builder.Append(c);
            }

            cells.Add(builder.ToString().Trim());

            // 行頭・行末の区切りで出来る空のセルを除く.

            if (text.StartsWith("|") && 0 < cells.Count)
            {
                cells.RemoveAt(0);
            }

            if (text.EndsWith("|") && !text.EndsWith("\\|") && 0 < cells.Count)
            {
                cells.RemoveAt(cells.Count - 1);
            }

            return cells.ToArray();
        }

        private static bool IsBlank(string line)
        {
            return string.IsNullOrWhiteSpace(line);
        }

        /// <summary> 行頭の空白の幅(タブは4桁) </summary>
        private static int GetIndentWidth(string line)
        {
            var width = 0;

            foreach (var c in line)
            {
                if (c == ' ')
                {
                    width++;
                }
                else if (c == '\t')
                {
                    width += TabWidth;
                }
                else
                {
                    break;
                }
            }

            return width;
        }

        /// <summary> 行頭の空白を指定の幅まで取り除く </summary>
        private static string RemoveIndent(string line, int width)
        {
            var removed = 0;
            var index = 0;

            while (index < line.Length && removed < width)
            {
                var c = line[index];

                if (c == ' ')
                {
                    removed++;
                }
                else if (c == '\t')
                {
                    removed += TabWidth;
                }
                else
                {
                    break;
                }

                index++;
            }

            return line.Substring(index);
        }
    }
}
