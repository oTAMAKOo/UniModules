
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

            var textComponent = UnityUtility.GetComponent<Text>(gameObject);

            if (textComponent != null)
            {
                textComponent.color = info.textColor;
            }

            if (info.hasOutline)
            {
                var applyColor = false;

                var textOutlineComponent = UnityUtility.GetComponent<TextOutline>(gameObject);

                if (textOutlineComponent != null)
                {
                    textOutlineComponent.SetColor(info.outlineColor);
                    applyColor = true;
                }

                var richTextOutlineComponent = UnityUtility.GetComponent<RichTextOutline>(gameObject);

                if (richTextOutlineComponent != null)
                {
                    richTextOutlineComponent.effectColor = info.outlineColor;
                    applyColor = true;
                }

                var outlineComponent = UnityUtility.GetComponent<Outline>(gameObject);

                if (outlineComponent != null)
                {
                    outlineComponent.effectColor = info.outlineColor;
                    applyColor = true;
                }

                if (!applyColor)
                {
                    var hierarchyPath = UnityUtility.GetChildHierarchyPath(null, gameObject);
                    Debug.LogWarningFormat("This object require outline category component.\n{0}/{1}", hierarchyPath, gameObject.name);
                }
            }

            if (info.hasShadow)
            {
                var applyColor = false;

                var textShadowComponent = UnityUtility.GetComponent<TextShadow>(gameObject);

                if (textShadowComponent != null)
                {
                    textShadowComponent.SetColor(info.shadowColor);
                    applyColor = true;
                }

                var richTextShadowComponent = UnityUtility.GetComponent<RichTextShadow>(gameObject);

                if (richTextShadowComponent != null)
                {
                    richTextShadowComponent.effectColor = info.shadowColor;
                    applyColor = true;
                }

                var shadowComponent = UnityUtility.GetComponent<Shadow>(gameObject);

                if (shadowComponent != null)
                {
                    shadowComponent.effectColor = info.shadowColor;
                    applyColor = true;
                }

                if (!applyColor)
                {
                    var hierarchyPath = UnityUtility.GetChildHierarchyPath(null, gameObject);
                    Debug.LogWarningFormat("This object require shadow category component.\n{0}/{1}", hierarchyPath, gameObject.name);
                }
            }
        }
    }
}
