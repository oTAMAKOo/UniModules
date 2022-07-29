using System.Text;
using System.Text.RegularExpressions;
using Extensions;

namespace TMPro
{
	public partial class RubyTextMeshProUGUI
	{
		/// <summary> ルビタグを含む行の先頭に空のルビタグを追加  </summary>
		public string InsertRubyTag(string text)
		{
			if (string.IsNullOrEmpty(text)){ return text; }

			var s1 = GetPreferredValues("\u00A0a").x;
			var s2 = GetPreferredValues("a").x;

			var spaceW = (s1  - s2) * rubyScale;
            
			var emptyRubyText = $"<space=-{spaceW}><ruby= ></ruby>";

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
	}
}
