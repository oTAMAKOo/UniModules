
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Modules.Devkit.Markdown
{
    /// <summary> インライン記法の描画設定 </summary>
    public sealed class MarkdownInlineStyle
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public Color LinkColor { get; set; }

        public Color CodeColor { get; set; }

        public Color CodeBackgroundColor { get; set; }

        //----- method -----

        public bool IsSame(MarkdownInlineStyle other)
        {
            if (other == null){ return false; }

            return LinkColor == other.LinkColor && CodeColor == other.CodeColor && CodeBackgroundColor == other.CodeBackgroundColor;
        }
    }

    /// <summary> Markdownのインライン記法をリッチテキストに変換する </summary>
    public static class MarkdownInline
    {
        //----- params -----

        private const string EscapableChars = "\\`*_{}[]()#+-.!|<>~";

        private const string UrlTrailingChars = ".,;:!?)]}、。）」";

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary> リッチテキストに変換する. リンクは links の添字を ID とする link タグになる. </summary>
        public static string ToRichText(string text, MarkdownInlineStyle style, List<string> links)
        {
            if (string.IsNullOrEmpty(text)){ return string.Empty; }

            var builder = new StringBuilder();

            Append(builder, text, style, links);

            return builder.ToString();
        }

        /// <summary> 記法を取り除いた表示文字数の目安 </summary>
        public static int GetDisplayLength(string text)
        {
            if (string.IsNullOrEmpty(text)){ return 0; }

            var length = 0;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '`' || c == '*'){ continue; }

                // リンクは表示名だけを数える.
                if (c == ']' && i + 1 < text.Length && text[i + 1] == '(')
                {
                    var close = text.IndexOf(')', i);

                    if (0 <= close)
                    {
                        i = close;
                        continue;
                    }
                }

                // 全角は2文字分として数える.
                length += c < 0x2000 ? 1 : 2;
            }

            return length;
        }

        private static void Append(StringBuilder builder, string text, MarkdownInlineStyle style, List<string> links)
        {
            var index = 0;

            while (index < text.Length)
            {
                var c = text[index];

                var next = index + 1 < text.Length ? text[index + 1] : '\0';

                // エスケープ.

                if (c == '\\' && 0 <= EscapableChars.IndexOf(next) && next != '\0')
                {
                    AppendEscaped(builder, next);
                    index += 2;
                    continue;
                }

                // コード.

                if (c == '`')
                {
                    var run = CountRun(text, index, '`');

                    var close = FindCodeClose(text, index + run, run);

                    if (0 <= close)
                    {
                        AppendCode(builder, text.Substring(index + run, close - (index + run)), style);
                        index = close + run;
                    }
                    else
                    {
                        builder.Append('`', run);
                        index += run;
                    }

                    continue;
                }

                // 画像(文中の画像は代替テキストを表示).

                if (c == '!' && next == '[')
                {
                    if (TryParseLink(text, index + 1, out var alt, out _, out var imageEnd))
                    {
                        AppendEscaped(builder, alt);
                        index = imageEnd;
                        continue;
                    }
                }

                // リンク.

                if (c == '[')
                {
                    if (TryParseLink(text, index, out var label, out var url, out var linkEnd))
                    {
                        AppendLinkOpen(builder, url, style, links);
                        Append(builder, label, style, links);
                        AppendLinkClose(builder);

                        index = linkEnd;
                        continue;
                    }
                }

                // 太字・斜体.

                if (c == '*' || c == '_')
                {
                    if (TryAppendEmphasis(builder, text, ref index, style, links)){ continue; }
                }

                // 打ち消し線.

                if (c == '~' && next == '~')
                {
                    var close = text.IndexOf("~~", index + 2, StringComparison.Ordinal);

                    if (index + 2 < close)
                    {
                        builder.Append("<s>");
                        Append(builder, text.Substring(index + 2, close - (index + 2)), style, links);
                        builder.Append("</s>");

                        index = close + 2;
                        continue;
                    }
                }

                // 山括弧で囲んだURL・改行タグ.

                if (c == '<')
                {
                    var close = text.IndexOf('>', index + 1);

                    if (0 <= close)
                    {
                        var inner = text.Substring(index + 1, close - (index + 1));

                        if (IsUrl(inner) && inner.IndexOf(' ') < 0)
                        {
                            AppendLinkOpen(builder, inner, style, links);
                            AppendEscaped(builder, inner);
                            AppendLinkClose(builder);

                            index = close + 1;
                            continue;
                        }

                        var tag = inner.Replace(" ", string.Empty).ToLowerInvariant();

                        if (tag == "br" || tag == "br/")
                        {
                            builder.Append('\n');

                            index = close + 1;
                            continue;
                        }
                    }
                }

                // URLの直書き.

                if (c == 'h' && (index == 0 || !IsWordChar(text[index - 1])) && IsUrlStart(text, index))
                {
                    var end = index;

                    while (end < text.Length && !char.IsWhiteSpace(text[end]) && text[end] != '<' && text[end] < 0x3000)
                    {
                        end++;
                    }

                    while (index < end && 0 <= UrlTrailingChars.IndexOf(text[end - 1]))
                    {
                        end--;
                    }

                    var url = text.Substring(index, end - index);

                    AppendLinkOpen(builder, url, style, links);
                    AppendEscaped(builder, url);
                    AppendLinkClose(builder);

                    index = end;
                    continue;
                }

                AppendEscaped(builder, c);

                index++;
            }
        }

        private static bool TryAppendEmphasis(StringBuilder builder, string text, ref int index, MarkdownInlineStyle style, List<string> links)
        {
            var c = text[index];

            var run = CountRun(text, index, c);

            // アンダースコアは単語の途中では記法として扱わない.
            if (c == '_' && 0 < index && IsWordChar(text[index - 1])){ return false; }

            if (2 <= run)
            {
                var marker = new string(c, 2);

                var close = text.IndexOf(marker, index + 2, StringComparison.Ordinal);

                if (close <= index + 2){ return false; }

                if (char.IsWhiteSpace(text[index + 2]) || char.IsWhiteSpace(text[close - 1])){ return false; }

                if (c == '_' && close + 2 < text.Length && IsWordChar(text[close + 2])){ return false; }

                builder.Append("<b>");
                Append(builder, text.Substring(index + 2, close - (index + 2)), style, links);
                builder.Append("</b>");

                index = close + 2;

                return true;
            }

            if (index + 1 < text.Length && !char.IsWhiteSpace(text[index + 1]))
            {
                var close = index + 2;

                while (close < text.Length)
                {
                    close = text.IndexOf(c, close);

                    if (close < 0){ return false; }

                    var isSingle = text[close - 1] != c && (text.Length <= close + 1 || text[close + 1] != c);

                    var isWordEnd = c != '_' || text.Length <= close + 1 || !IsWordChar(text[close + 1]);

                    if (isSingle && isWordEnd && !char.IsWhiteSpace(text[close - 1])){ break; }

                    close++;
                }

                if (text.Length <= close){ return false; }

                builder.Append("<i>");
                Append(builder, text.Substring(index + 1, close - (index + 1)), style, links);
                builder.Append("</i>");

                index = close + 1;

                return true;
            }

            return false;
        }

        /// <summary> [表示名](URL) を解析する </summary>
        private static bool TryParseLink(string text, int index, out string label, out string url, out int end)
        {
            label = null;
            url = null;
            end = index;

            // 対応する閉じ括弧を探す.

            var depth = 0;
            var labelEnd = -1;

            for (var i = index; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '\\')
                {
                    i++;
                    continue;
                }

                if (c == '[')
                {
                    depth++;
                }
                else if (c == ']')
                {
                    depth--;

                    if (depth == 0)
                    {
                        labelEnd = i;
                        break;
                    }
                }
            }

            if (labelEnd < 0){ return false; }

            if (text.Length <= labelEnd + 1 || text[labelEnd + 1] != '('){ return false; }

            // URL内の括弧の入れ子を考慮して閉じ括弧を探す.

            depth = 0;

            var urlEnd = -1;

            for (var i = labelEnd + 1; i < text.Length; i++)
            {
                var c = text[i];

                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;

                    if (depth == 0)
                    {
                        urlEnd = i;
                        break;
                    }
                }
            }

            if (urlEnd < 0){ return false; }

            label = text.Substring(index + 1, labelEnd - (index + 1));
            url = text.Substring(labelEnd + 2, urlEnd - (labelEnd + 2)).Trim();

            // タイトル指定を取り除く.

            var titleIndex = url.IndexOf(" \"", StringComparison.Ordinal);

            if (0 <= titleIndex)
            {
                url = url.Substring(0, titleIndex).Trim();
            }

            if (url.StartsWith("<") && url.EndsWith(">"))
            {
                url = url.Substring(1, url.Length - 2);
            }

            end = urlEnd + 1;

            return true;
        }

        private static void AppendLinkOpen(StringBuilder builder, string url, MarkdownInlineStyle style, List<string> links)
        {
            links.Add(url);

            builder.Append("<link=\"").Append(links.Count - 1).Append("\">");
            builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(style.LinkColor)).Append("><u>");
        }

        private static void AppendLinkClose(StringBuilder builder)
        {
            builder.Append("</u></color></link>");
        }

        private static void AppendCode(StringBuilder builder, string code, MarkdownInlineStyle style)
        {
            // 前後が空白で囲まれている場合は1文字ずつ取り除く.
            if (2 < code.Length && code[0] == ' ' && code[code.Length - 1] == ' ' && code.Trim().Length != 0)
            {
                code = code.Substring(1, code.Length - 2);
            }

            builder.Append("<mark=#").Append(ColorUtility.ToHtmlStringRGBA(style.CodeBackgroundColor)).Append(">");
            builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(style.CodeColor)).Append(">");

            AppendEscaped(builder, code);

            builder.Append("</color></mark>");
        }

        private static void AppendEscaped(StringBuilder builder, string text)
        {
            foreach (var c in text)
            {
                AppendEscaped(builder, c);
            }
        }

        /// <summary> リッチテキストのタグとして解釈されないように出力する </summary>
        private static void AppendEscaped(StringBuilder builder, char c)
        {
            if (c == '<')
            {
                builder.Append("<noparse><</noparse>");
            }
            else
            {
                builder.Append(c);
            }
        }

        private static int CountRun(string text, int index, char c)
        {
            var count = 0;

            while (index + count < text.Length && text[index + count] == c)
            {
                count++;
            }

            return count;
        }

        /// <summary> 開始と同じ長さのバッククォートの並びを探す </summary>
        private static int FindCodeClose(string text, int index, int run)
        {
            while (index < text.Length)
            {
                index = text.IndexOf('`', index);

                if (index < 0){ return -1; }

                var count = CountRun(text, index, '`');

                if (count == run){ return index; }

                index += count;
            }

            return -1;
        }

        private static bool IsUrl(string text)
        {
            return IsUrlStart(text, 0) || text.StartsWith("mailto:");
        }

        private static bool IsUrlStart(string text, int index)
        {
            return string.CompareOrdinal(text, index, "http://", 0, 7) == 0 || string.CompareOrdinal(text, index, "https://", 0, 8) == 0;
        }

        private static bool IsWordChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_';
        }
    }
}
