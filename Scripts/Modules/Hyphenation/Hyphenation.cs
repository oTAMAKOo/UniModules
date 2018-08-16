
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Extensions;

namespace Modules.Hyphenation
{
    public static class Hyphenation
    {
        //--------------------------------------------------------------------------
        // 禁則処理. 
        // http://ja.wikipedia.org/wiki/%E7%A6%81%E5%89%87%E5%87%A6%E7%90%86
        //--------------------------------------------------------------------------

        // 行頭禁則文字.
        private static readonly char[] HYP_FRONT =
            (",)]｝、。）〕〉》」』】〙〗〟’”｠»" +             // 終わり括弧類 簡易版.
             "ァィゥェォッャュョヮヵヶっぁぃぅぇぉっゃゅょゎ" +   // 行頭禁則和字.
             "‐゠–〜ー" +                                        // ハイフン類.
             "?!！？‼⁇⁈⁉" +                                       // 区切り約物.
             ":;" +                                               // 中点類.
             "。.")                                               // 句点類.
            .ToCharArray();

        private static readonly char[] HYP_BACK =
            "(（[｛〔〈《「『【〘〖〝‘“｟«"                     //　始め括弧類.
            .ToCharArray();

        private static readonly char[] HYP_LATIN =
            ("abcdefghijklmnopqrstuvwxyz" +
             "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
             "0123456789" +
             "<>=/().,")
            .ToCharArray();

        private static readonly char LineEndChar = '\n';

        /// <summary> テキストに禁則処理を適用 </summary>
        public static string Format(string text)
        {
            var lines = text
                .FixLineEnd(LineEndChar.ToString())
                .Split(LineEndChar)
                .ToList();

            // 1行の時は処理しない.
            if (lines.Count <= 1) { return text; }

            var builder = new StringBuilder();

            // 行頭禁則.
            FormatHyphenationFront(ref lines);

            // 行末禁則.
            FormatHyphenationBack(ref lines);

            // 再構築.
            for (var i = 0; i < lines.Count; i++)
            {
                if (i != lines.Count - 1)
                {
                    builder.AppendLine(lines[i]);
                }
                else
                {
                    builder.Append(lines[i]);
                }
            }

            return builder.ToString();
        }

        // 行頭禁則.
        private static void FormatHyphenationFront(ref List<string> lines)
        {
            var loop = false;

            do
            {
                loop = false;

                for (var i = 0; i < lines.Count; i++)
                {
                    var change = false;

                    if (i == 0) { continue; }

                    var lineChar = lines[i].ToCharArray().ToList();

                    var prev = new StringBuilder(lines[i - 1]);

                    while (lineChar.Any())
                    {
                        var c = lineChar.FirstOrDefault();

                        if (!CheckHyphenationFront(c)) { break; }

                        prev.Append(c);
                        lineChar.RemoveAt(0);
                        change = true;
                    }

                    var line = new string(lineChar.ToArray());

                    lines[i - 1] = prev.ToString();

                    // 編集した結果空文字になったら詰める.
                    if (change && string.IsNullOrEmpty(line))
                    {
                        lines.RemoveAt(i);
                        loop = true;
                        break;
                    }

                    lines[i] = line;
                }
            }
            while (loop);
        }

        // 行末禁則.
        private static void FormatHyphenationBack(ref List<string> lines)
        {
            var loop = false;

            do
            {
                loop = false;

                for (var i = 0; i < lines.Count - 1; i++)
                {
                    var change = false;

                    var lineChar = lines[i].ToCharArray().ToList();

                    var next = new StringBuilder(lines[i + 1]);

                    while (lineChar.Any())
                    {
                        var c = lineChar.LastOrDefault();

                        if (!CheckHyphenationBack(c)) { break; }

                        next.Insert(0, c);
                        lineChar.RemoveAt(lineChar.Count - 1);
                        change = true;
                    }

                    var line = new string(lineChar.ToArray());

                    lines[i + 1] = next.ToString();

                    // 編集した結果空文字になったら詰める.
                    if (change && string.IsNullOrEmpty(line))
                    {
                        lines.RemoveAt(i);
                        loop = true;
                        break;
                    }

                    lines[i] = line;
                }
            }
            while (loop);
        }

        public static bool CheckHyphenationFront(char str)
        {
            return Array.Exists(HYP_FRONT, item => item == str);
        }

        public static bool CheckHyphenationBack(char str)
        {
            return Array.Exists(HYP_BACK, item => item == str);
        }

        public static bool IsLatin(char s)
        {
            return Array.Exists(HYP_LATIN, item => item == s);
        }
    }
}
