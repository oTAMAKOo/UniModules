﻿﻿﻿﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text.RegularExpressions;
using Extensions;

namespace Modules.UI
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Text))]
    public sealed class RichTextAlpha : MonoBehaviour
    {
        //----- params -----

        // 大文字小文字の区別、二重引用符の有無など、リッチテキストの仕様に準拠.
        private const string Pattern = "(<color=\"?#[a-f0-9]{6})([a-f0-9]{2})?(\"?>)";

        //----- field -----

        private Text text = null;
        private Regex regex = null;

        private string prevText = null;
        private float? prevAlpha = null;

        //----- property -----

        //----- method -----
        
        void OnEnable()
        {
            UpdateAlpha();
        }

        void LateUpdate()
        {
            UpdateAlpha();
        }

        public void UpdateAlpha()
        {
            if(text == null)
            {
                text = UnityUtility.GetComponent<Text>(gameObject);
            }
            
            if (!text.supportRichText) { return; }

            if (prevText == text.text && prevAlpha == text.color.a) { return; }

            ApplyAlpha(text.color.a);

            prevText = text.text;
            prevAlpha = text.color.a;
        }

        private void ApplyAlpha(float alpha)
        {
            var alpha16 = ConvertTo16(alpha);

            var replacement = "${1}" + alpha16 + "${3}";

            if(regex == null)
            {
                regex = new Regex(Pattern, RegexOptions.IgnoreCase);
            }

            text.text = regex.Replace(text.text, replacement);
        }

        private string ConvertTo16(float alpha)
        {
            // Text.colorの持つアルファ値は0f-1f.
            // 16進数に変換するために0-255の整数型に変換.
            var alpha10 = (int)(alpha * 255f);

            var alpha16 = Convert.ToString(alpha10, 16);

            // 16進数で一桁の場合、二桁目を0詰.
            if (alpha10 < 16) { alpha16 = "0" + alpha16; }

            return alpha16;
        }
    }
}
