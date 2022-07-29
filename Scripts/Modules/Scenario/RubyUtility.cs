
using System.Text;
using System.Text.RegularExpressions;
using Extensions;

namespace Modules.Scenario
{
    public static class RubyUtility
    {
		/// <summary> ルビタグを含む行の先頭に空のルビタグを追加  </summary>
		public static string InsertRubyTag(string text)
		{
			const string EmptyRubyText = "<ruby= ></ruby>";
			
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
					builder.Append(EmptyRubyText);
				}
				
				builder.Append(line);
			}

			return builder.ToString();
		}
    }
}