
using UnityEngine;
using System;
using System.Linq;

namespace Modules.TagTect
{
    public sealed class RubyTagText : TagText
    {
        //----- params -----

        //----- field -----

        //----- property -----

		public bool SyncRuby { get; set; } = false;

        //----- method -----

		protected override Info[] EditTextInfos(Info[] infos)
		{
			for (var i = 0; i < infos.Length; i++)
			{
				try
				{
					var info = infos[i];

					if (info.Type != ContentType.Tag){ continue; }

					if (string.IsNullOrEmpty(info.Param)){ continue; }

					if (!IsRubyTag(info)){ continue; }

					// ルビ対象文字列を検索.

					Info rubyTarget = null;

					for (var j = i; j < infos.Length; j++)
					{
						var temp = infos[j];

						if (temp.Type == ContentType.Text)
						{
							rubyTarget = temp;
							break;
						}

						if(temp.Type == ContentType.CloseTag && IsRubyTag(temp))
						{
							break;
						}
					}

					// 表示されているルビ対象文字列の長さに応じてルビを表示.

					var currentLength = 0;
					var fullLength = 0;

					if (rubyTarget != null)
					{
						var originInfo = parsedInfos.FirstOrDefault(x => x.Identifier == rubyTarget.Identifier);
						
						currentLength = rubyTarget.Text.Length;
						fullLength = originInfo.Text.Length;
					}

					if (fullLength != 0)
					{
						var ruby = string.Empty;

						if (SyncRuby)
						{
							var len = (int)Math.Floor(info.Param.Length * ((float)currentLength / fullLength));
							
							ruby = info.Param.Substring(0, len);
						}
						else
						{
							ruby = currentLength == fullLength ? info.Param : string.Empty;
						}

						info.Text = $"<{info.Tag}={ruby}>";
					}
				}
				catch (Exception e)
				{
					Debug.LogException(e);
				}
			}

			return infos;
		}
		
		private bool IsRubyTag(Info info)
		{
			return info.Tag == "ruby" || info.Tag == "r";
		}
    }
}