
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using Extensions;

namespace Modules.Hyphenation
{
    // ※ BestFitと併用すると相互にサイズをし合い最終サイズが正しく取得できない為、併用しない.

    [ExecuteAlways]
    [RequireComponent(typeof(Text))]
    public sealed class UITextHyphenation : UIBehaviour
    {
        //----- params -----

        private static readonly string RITCH_TEXT_REPLACE =
            "(\\<color=.*\\>|</color>|" +
            "\\<size=.n\\>|</size>|" +
            "<b>|</b>|" +
            "<i>|</i>)";

        //----- field -----

        private Text target = null;
        private RectTransform rectTransform = null;
        private string originText = null;
        private string fixedText = null;
        private float? fixedWidth = null;

        //----- property -----

        public Text Text { get { return target ?? (target = UnityUtility.GetComponent<Text>(gameObject)); } }

        private RectTransform RectTransform
        {
            get { return rectTransform ?? (rectTransform = UnityUtility.GetComponent<RectTransform>(gameObject)); }
        }

        public float textWidth
        {
            set { RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, value); }

            get { return RectTransform.rect.width; }
        }

        //----- method -----

        // ※ Updateだと適用が反映されないのでLateUpdateで反映.
        void LateUpdate()
        {
            if (Text == null) { return; }

            if (RectTransform == null) { return; }

            // ObserveEveryValueChangedで監視するとComponentが追加されてしまうのでUpdateで監視.
            if (fixedText != Text.text)
            {
                UpdateText(Text.text);
            }
        }

        public void UpdateText(string text)
        {
            if (fixedText == text && fixedWidth == textWidth) { return; }

            originText = text;

            if (string.IsNullOrEmpty(originText))
            {
                fixedText = text;
                fixedWidth = null;
            }
            else
            {
                fixedText = GetFormatedText(originText.FixLineEnd());
                fixedWidth = textWidth;
            }

            Text.text = fixedText;
        }

        private float GetSpaceWidth()
        {
            var tmp0 = GetTextWidth("m m");
            var tmp1 = GetTextWidth("mm");

            return (tmp0 - tmp1);
        }

        private float GetTextWidth(string message)
        {
            if (Text.supportRichText)
            {
                message = Regex.Replace(message, RITCH_TEXT_REPLACE, string.Empty, RegexOptions.IgnoreCase);
            }

            Text.text = message;

            return Text.preferredWidth;
        }

        private string GetFormatedText(string text)
        {
            if (string.IsNullOrEmpty(text)) { return string.Empty; }

            if (RectTransform == null) { return string.Empty; }

            var rectWidth = RectTransform.rect.width;
            var spaceCharacterWidth = GetSpaceWidth();

            // override
            Text.horizontalOverflow = HorizontalWrapMode.Overflow;

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
    }
}
