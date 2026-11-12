
using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Modules.Devkit.Markdown
{
    /// <summary> 整形表示の対象と、適用するスタイルシートの組 </summary>
    [Serializable]
    public sealed class MarkdownViewRule
    {
        //----- params -----

        //----- field -----

        [Tooltip("File name with extension. '*' matches any characters.")]
        [SerializeField]
        private string fileName = null;

        [Tooltip("Style sheet to apply. Uses the default style sheet when empty.")]
        [SerializeField]
        private StyleSheet styleSheet = null;

        //----- property -----

        /// <summary> 対象のファイル名(拡張子を含む). '*' は任意の文字列に一致する. </summary>
        public string FileName { get { return fileName; } }

        /// <summary> 適用するスタイルシート. 未設定の場合は標準のスタイルシートを適用する. </summary>
        public StyleSheet StyleSheet { get { return styleSheet; } }

        //----- method -----

        public MarkdownViewRule(){ }

        public MarkdownViewRule(string fileName, StyleSheet styleSheet)
        {
            this.fileName = fileName;
            this.styleSheet = styleSheet;
        }

        /// <summary> ファイル名がこのルールの対象か </summary>
        public bool IsMatch(string targetFileName)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(targetFileName)){ return false; }

            if (fileName.IndexOf('*') < 0)
            {
                return string.Equals(fileName, targetFileName, StringComparison.OrdinalIgnoreCase);
            }

            var pattern = "^" + Regex.Escape(fileName).Replace("\\*", ".*") + "$";

            return Regex.IsMatch(targetFileName, pattern, RegexOptions.IgnoreCase);
        }
    }

    /// <summary>
    /// 整形表示の対象を判定する.
    /// MarkdownViewConfig のルールを先に判定し、当たらない場合は DefaultMarkdownViewConfig のルールを判定する.
    /// </summary>
    public static class MarkdownViewRules
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary> アセットパスに当たるルールを取得する. 対象外なら null を返す. </summary>
        public static MarkdownViewRule Find(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath)){ return null; }

            var fileName = Path.GetFileName(assetPath);

            var config = MarkdownViewConfig.FindInstance();

            if (config != null)
            {
                var rule = FindRule(config.Rules, fileName);

                if (rule != null){ return rule; }
            }

            var defaultConfig = DefaultMarkdownViewConfig.Instance;

            if (defaultConfig != null)
            {
                return FindRule(defaultConfig.Rules, fileName);
            }

            return null;
        }

        /// <summary> ルールのスタイルシートを取得する. 未設定の場合は標準のスタイルシートを返す. </summary>
        public static StyleSheet LoadStyleSheet(MarkdownViewRule rule)
        {
            if (rule != null && rule.StyleSheet != null){ return rule.StyleSheet; }

            var defaultConfig = DefaultMarkdownViewConfig.Instance;

            if (defaultConfig == null){ return null; }

            return defaultConfig.DefaultStyleSheet;
        }

        private static MarkdownViewRule FindRule(MarkdownViewRule[] rules, string fileName)
        {
            foreach (var rule in rules)
            {
                if (rule != null && rule.IsMatch(fileName)){ return rule; }
            }

            return null;
        }
    }
}
