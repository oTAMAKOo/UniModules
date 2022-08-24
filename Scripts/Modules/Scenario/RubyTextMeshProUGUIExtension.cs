
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Extensions;

namespace TMPro
{
    public static class RubyTextMeshProUGUIExtension
    {
        private static FieldInfo rubyScaleFieldInfo = null;
        private static FieldInfo maxFontSizeFieldInfo = null;

        /// <summary> ルビタグを含む行の先頭に空のルビタグを追加  </summary>
        public static string InsertEmptyRubyTag(this RubyTextMeshProUGUI self, string text)
        {
            if (string.IsNullOrEmpty(text)){ return text; }

            // 空白のルビタグ作成.

            var fontSizeScale = 1f;

            if (self.enableAutoSizing)
            {
                fontSizeScale = self.fontSize / GetMaxFontSize(self);
            }

            var rightToLeft = self.isRightToLeftText ? 1 : -1;

            var s1 = self.GetPreferredValues("\u00A0a").x;
            var s2 = self.GetPreferredValues("a").x;

            var rubyScale = GetRubyScale(self);

            var spaceW = (s1 - s2) * rubyScale * fontSizeScale * rightToLeft;

            var emptyRubyText = $"<space=-{spaceW}><ruby= ></ruby>";

            // 文字列解析.

            var builder = new StringBuilder();

            var lines = text.FixLineEnd().Split('\n');
            
            var rubyTagRegex = new Regex("<ruby=[^>]*?>", RegexOptions.Compiled);

            foreach (var line in lines)
            {
                if (0 < builder.Length)
                {
                    builder.AppendLine();
                }

                if (rubyTagRegex.IsMatch(line))
                {
                    builder.Append(emptyRubyText);
                }
				
                builder.Append(line);
            }

            return builder.ToString();
        }

        private static float GetMaxFontSize(RubyTextMeshProUGUI textMeshPro)
        {
            if (rubyScaleFieldInfo == null)
            {
                var type = typeof(RubyTextMeshProUGUI);
                var flag = BindingFlags.NonPublic | BindingFlags.Instance;

                maxFontSizeFieldInfo = Reflection.GetFieldInfo(type, "m_maxFontSize", flag);
            }

            return (float)maxFontSizeFieldInfo.GetValue(textMeshPro);
        }

        private static float GetRubyScale(RubyTextMeshProUGUI textMeshPro)
        {
            if (rubyScaleFieldInfo == null)
            {
                var type = typeof(RubyTextMeshProUGUI);
                var flag = BindingFlags.NonPublic | BindingFlags.Instance;

                rubyScaleFieldInfo = Reflection.GetFieldInfo(type, "rubyScale", flag);
            }

            return (float)rubyScaleFieldInfo.GetValue(textMeshPro);
        }
    }
}
