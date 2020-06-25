
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.TextColorTag
{
    [ExecuteAlways]
    [RequireComponent(typeof(Text))]
    public sealed class TextColorTag : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private TextColorTagSetting setting = null;

        private Text textComponent = null;

        private string prevText = null;

        //----- property -----

        //----- method -----

        private void OnEnable()
        {
            UpdateText();
        }

        void LateUpdate()
        {
            UpdateText();
        }

        public void UpdateText()
        {
            if (setting == null) { return; }

            if (textComponent == null)
            {
                textComponent = UnityUtility.GetComponent<Text>(gameObject);
            }

            if (string.IsNullOrEmpty(textComponent.text)) { return; }

            if (textComponent.text != prevText)
            {
                textComponent.text = ConvertColorTag(textComponent.text);

                prevText = textComponent.text;

                Debug.Log("update");
            }
        }

        private string ConvertColorTag(string text)
        {
            var colorTaginfos = setting.GeTextColorTagInfos();

            if (colorTaginfos.IsEmpty()) { return text; }

            foreach (var colorTaginfo in colorTaginfos)
            {
                var from = string.Format("<c={0}>", colorTaginfo.tag);
                var to = string.Format("<color=#{0}>", ColorUtility.ToHtmlStringRGBA(colorTaginfo.color));

                text = text.Replace(from, to);
            }

            text = text.Replace("</c>", "</color>");

            return text;
        }
    }
}
