
using UnityEngine;
using System.Text.RegularExpressions;
using Extensions;
using TMPro;

namespace Modules.Hyphenation
{
	[ExecuteAlways]
	[RequireComponent(typeof(TextMeshProUGUI))]
    public sealed class TextMeshProHyphenation : TextHyphenationBase
    {
        //----- params -----

        //----- field -----

		private TextMeshProUGUI textComponent = null;

		//----- property -----

		private TextMeshProUGUI TextComponent
		{
			get { return textComponent ?? (textComponent = UnityUtility.GetComponent<TextMeshProUGUI>(gameObject)); }
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
			if (TextComponent.richText)
			{
				message = Regex.Replace(message, RitchTextReplace, string.Empty, RegexOptions.IgnoreCase);
			}

			Current = message;

			return TextComponent.preferredWidth;
		}

		protected override void SetTextOverflow()
		{
			TextComponent.overflowMode = TextOverflowModes.Overflow;
		}
    }
}