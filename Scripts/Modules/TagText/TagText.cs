
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Extensions;

namespace Modules.TagTect
{
    public class TagText
    {
        //----- params -----

		protected enum ContentType
		{
			Text,
			Tag,
			CloseTag,
		}

		protected class Info
		{
			public int Identifier { get; set; }
			public ContentType Type { get; set; }
			public string Text { get; set; }
			public string Tag { get; set; }
			public string Param { get; set; }

			public Info(){}

			public Info(Info source)
			{
				Identifier = source.Identifier;
				Type = source.Type;
				Text = source.Text;
				Tag = source.Tag;
				Param = source.Param;
			}
		}

        //----- field -----

		protected string origin = null;

		protected Info[] parsedInfos = null;

		private int identifier = 0;

		//----- property -----

		public int Length { get; private set; }

        //----- method -----

		public virtual void SetText(string origin)
		{
			this.origin = origin;

			identifier = 0;
			
			parsedInfos = Parse(origin);

			Length = parsedInfos.Where(x => x.Type == ContentType.Text).Sum(x => x.Text.Length) + 1;
		}

		public string Get(int length = -1)
		{
			if (length == -1){ return origin; }
			
			length = Math.Min(Math.Max(length, 0), Length);
			
			// 文字送りタグ文字列情報構築.

			var infos = BuildTagTextInfos(length);

			// 文字列情報編集.

			var editInfos = EditTextInfos(infos);
			
			// 文字列構築.

			var text = BuildTagText(editInfos);

			return text;
		}

		private Info[] Parse(string text)
		{
			var list = new List<Info>();

			var parts = Regex.Split(text, "(<[^>]*?>)", RegexOptions.None)
				.Where(x => !string.IsNullOrEmpty(x))
				.ToArray();

			foreach (var item in parts)
			{
				var info = new Info()
				{
					Identifier = identifier,
					Text = item
				};

				identifier++;

				if (item.StartsWith("<"))
				{
					info.Type = item.StartsWith("</") ? ContentType.CloseTag : ContentType.Tag;

					var content = item.TrimStart('<').TrimEnd('>');

					if (info.Type == ContentType.CloseTag)
					{
						content = content.TrimStart('/');
					}

					var start = content.IndexOf("=", StringComparison.CurrentCulture);

					info.Tag = 0 < start ? content.Substring(0 , start) : content;

					if (start != -1)
					{
						info.Param = content.Substring(start + 1);
					}
				}

				list.Add(info);
			}

			return list.ToArray();
		}
		
		/// <summary> 文字送りタグ文字列情報構築 </summary>
		private Info[] BuildTagTextInfos(int length)
		{
			var len = 0;

			var infos = new List<Info>();
			var opendTags = new List<Info>();

			var textBuilder = new StringBuilder();

			foreach (var item in parsedInfos)
			{
				switch (item.Type)
				{
					case ContentType.Text:
						{
							textBuilder.Clear();

							var info = new Info(item)
							{
								Text = string.Empty,
							};

							if (len < length)
							{
								foreach (var c in item.Text)
								{
									textBuilder.Append(c);

									len++;

									if (length <= len) { break; }
								}
							}

							info.Text = textBuilder.ToString();

							infos.Add(info);
						}
						break;
					case ContentType.Tag:
						{
							opendTags.Add(item);

							infos.Add(item);
						}
						break;
					case ContentType.CloseTag:
						{
							var remove = opendTags.LastOrDefault(x => x.Tag == item.Tag);

							opendTags.Remove(remove);

							infos.Add(item);
						}
						break;
				}

				if (opendTags.IsEmpty() && length <= len)
				{
					break;
				}
			}

			return infos.ToArray();
		}

		/// <summary> タグ文字列情報から文字列構築 </summary>
		private string BuildTagText(Info[] infos)
		{
			var builder = new StringBuilder();

			foreach (var info in infos)
			{
				builder.Append(info.Text);
			}

			return builder.ToString();
		}

		protected virtual Info[] EditTextInfos(Info[] infos) { return infos; }
	}
}