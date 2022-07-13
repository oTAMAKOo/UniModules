
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using Extensions;

namespace Modules.Hyphenation
{
    // ※ BestFitと併用すると相互にサイズをし合い最終サイズが正しく取得できない為、併用しない.

    [ExecuteAlways]
    [RequireComponent(typeof(Text))]
    public sealed class TextHyphenation : TextHyphenationBase
    {
		//----- params -----

		//----- field -----

		private Text textComponent = null;

		//----- property -----

		private Text TextComponent
		{
			get { return textComponent ?? (textComponent = UnityUtility.GetComponent<Text>(gameObject)); }
		}

		protected override string Current
		{
			get { return TextComponent.text; }
			set { TextComponent.text = value; }
		}

		protected override bool HasTextComponent
		{
			get { return textComponent != null; }
		}

		//----- method -----

		protected override float GetTextWidth(string message)
		{
			if (TextComponent.supportRichText)
			{
				message = Regex.Replace(message, RITCH_TEXT_REPLACE, string.Empty, RegexOptions.IgnoreCase);
			}

			Current = message;

			return TextComponent.preferredWidth;
		}

		protected override void SetTextOverflow()
		{
			TextComponent.horizontalOverflow = HorizontalWrapMode.Overflow;
		}
	}
}
