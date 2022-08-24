
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Text;
using Extensions;

namespace Modules.Hyphenation
{
    public abstract class TextHyphenationBase : UIBehaviour
    {
        //----- params -----

		protected static readonly string RITCH_TEXT_REPLACE =
            "(\\<color=.*\\>|</color>|" +
            "\\<size=.n\\>|</size>|" +
			"\\<link=.n\\>|</link>|" +
            "<b>|</b>|" +
            "<i>|</i>)";

        //----- field -----

		private RectTransform rectTransform = null;

		protected string originText = null;
		protected string fixedText = null;
		protected float? fixedWidth = null;

        //----- property -----
		
		protected RectTransform RectTransform
		{
			get { return rectTransform ?? (rectTransform = UnityUtility.GetComponent<RectTransform>(gameObject)); }
		}

		public float TextWidth
		{
			set { RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value); }

			get { return RectTransform.rect.width; }
		}

		public string RitchTextReplace { get; set; } = RITCH_TEXT_REPLACE;

		protected abstract string Current { get; set; }

		protected abstract bool HasTextComponent { get; }

        //----- method -----

		// ※ Updateだと適用が反映されないのでLateUpdateで反映.
		void LateUpdate()
		{
			if (!HasTextComponent) { return; }

			if (RectTransform == null) { return; }

			// ObserveEveryValueChangedで監視するとComponentが追加されてしまうのでUpdateで監視.
			if (fixedText != Current)
			{
				UpdateText(Current);
			}
		}

		public void UpdateText(string text)
        {
            if (fixedText == text && fixedWidth == TextWidth) { return; }

            originText = text;

            if (string.IsNullOrEmpty(originText))
            {
                fixedText = text;
                fixedWidth = null;
            }
            else
            {
                fixedText = GetFormatedText(originText.FixLineEnd());
                fixedWidth = TextWidth;
            }

			Current = fixedText;
        }

        private float GetSpaceWidth()
        {
            var tmp0 = GetTextWidth("m m");
            var tmp1 = GetTextWidth("mm");

            return (tmp0 - tmp1);
        }

		private string GetFormatedText(string text)
        {
            if (string.IsNullOrEmpty(text)) { return string.Empty; }

            if (RectTransform == null) { return string.Empty; }

            var rectWidth = RectTransform.rect.width;
            var spaceCharacterWidth = GetSpaceWidth();
			
			SetTextOverflow();

            var lineBuilder = new StringBuilder();

            var lineWidth = 0f;

            var words = GetWordList(text);

            foreach (var word in words)
            {
                var width = GetTextWidth(word);

                lineWidth += width;

                if (word == "\n")
                {
                    lineWidth = 0;
                }
                else
                {
                    if (word == " ")
                    {
                        lineWidth += spaceCharacterWidth;
                    }

                    if (lineWidth > rectWidth)
                    {
                        lineBuilder.Append("\n");
                        lineWidth = width;
                    }
                }

                lineBuilder.Append(word);
            }

            return lineBuilder.ToString();
        }

        private string[] GetWordList(string tmpText)
        {
            var words = new List<string>();
            var line = new StringBuilder();
            var emptyChar = new char();

            for (var characterCount = 0; characterCount < tmpText.Length; characterCount++)
            {
                var currentCharacter = tmpText[characterCount];
                var nextCharacter = (characterCount < tmpText.Length - 1) ? tmpText[characterCount + 1] : emptyChar;
                var prevCharacter = (characterCount > 0) ? tmpText[characterCount - 1] : emptyChar;

                line.Append(currentCharacter);

                var isLatinCurrent = Hyphenation.IsLatin(currentCharacter);
                var isLatinPrev = Hyphenation.IsLatin(prevCharacter);
                var isLatinNext = Hyphenation.IsLatin(nextCharacter);
                var isLineEndChar = Hyphenation.IsLineEndChar(currentCharacter);

                var hyphenationBackCurrent = Hyphenation.CheckHyphenationBack(currentCharacter);
                var hyphenationBackPrev = Hyphenation.CheckHyphenationBack(prevCharacter);
                var hyphenationFrontNext = Hyphenation.CheckHyphenationFront(nextCharacter);

                if ((isLatinCurrent && isLatinPrev) && (isLatinCurrent && !isLatinPrev) ||
                    (!isLatinCurrent && hyphenationBackPrev) ||
                    (!isLatinNext && !hyphenationFrontNext && !hyphenationBackCurrent) ||
                    isLineEndChar || (characterCount == tmpText.Length - 1))
                {
                    words.Add(line.ToString());
                    line = new StringBuilder();
                }
            }

            return words.ToArray();
        }

		protected abstract float GetTextWidth(string message);

		protected abstract void SetTextOverflow();
    }
}