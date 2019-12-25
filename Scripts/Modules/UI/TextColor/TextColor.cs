
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Modules.UI.TextEffect;

namespace Modules.UI.TextColor
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Text))]
    public sealed class TextColor : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private TextColorSetting setting = null;
        [SerializeField]
        private string selectionGuid = null;

        //----- property -----

        public TextColorSetting Setting
        {
            get { return setting; }

            set { setting = value; }
        }

        public string TextColorName
        {
            get
            {
                if (setting == null) { return null; }

                var info = setting.GetTextColorInfo(selectionGuid);

                return info != null ? info.name : null;
            }

            set
            {
                if (setting == null) { return; }

                var colorInfo = setting.ColorInfos.FirstOrDefault(x => x.name == value);

                selectionGuid = colorInfo != null ? colorInfo.guid : null;
            }
        }

        //----- method -----

        private void OnEnable()
        {
            ApplyColor();
        }

        public void ApplyColor()
        {
            if (string.IsNullOrEmpty(selectionGuid)) { return; }

            var info = setting.GetTextColorInfo(selectionGuid);

            if (info == null) { return; }

            var components = UnityUtility.GetComponents<Component>(gameObject).ToArray();
            
            var textComponent = FindComponent<Text>(components);

            if (textComponent != null)
            {
                textComponent.color = info.textColor;
            }

            if (info.hasOutline)
            {
                var textOutlineComponent = FindComponent<TextOutline>(components);

                if (textOutlineComponent != null)
                {
                    textOutlineComponent.SetColor(info.outlineColor);
                }

                var richTextOutlineComponent = FindComponent<RichTextOutline>(components);

                if (richTextOutlineComponent != null)
                {
                    richTextOutlineComponent.effectColor = info.outlineColor;
                }

                var outlineComponent = FindComponent<Outline>(components);

                if (outlineComponent != null)
                {
                    outlineComponent.effectColor = info.outlineColor;
                }
            }

            if (info.hasShadow)
            {
                var textShadowComponent = FindComponent<TextShadow>(components);

                if (textShadowComponent != null)
                {
                    textShadowComponent.SetColor(info.shadowColor);
                }

                var richTextShadowComponent = FindComponent<RichTextShadow>(components);

                if (richTextShadowComponent != null)
                {
                    richTextShadowComponent.effectColor = info.shadowColor;
                }

                var shadowComponent = FindComponent<Shadow>(components);

                if (shadowComponent != null)
                {
                    shadowComponent.effectColor = info.shadowColor;
                }             
            }
        }

        private T FindComponent<T>(Component[] components)
        {
            return components.Where(x => x.GetType() == typeof(T)).Cast<T>().FirstOrDefault();
        }
    }
}
