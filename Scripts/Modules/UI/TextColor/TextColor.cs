
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Extensions;
using TMPro;

namespace Modules.UI.TextColor
{
    [ExecuteAlways]
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

                var colorInfo = GetColorInfo(value);

                var guid = colorInfo != null ? colorInfo.guid : null;

                if (guid != null)
                {
                    if (selectionGuid != guid)
                    {
                        selectionGuid = guid;

                        ApplyColor();
                    }
                }
                else
                {
                    Debug.LogErrorFormat("Color name {0} is not found.", value);
                }
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

            var textMeshProComponent = FindComponent<TextMeshProUGUI>(components);

            if (textMeshProComponent != null)
            {
                textMeshProComponent.color = info.textColor;
            }

            if (info.hasOutline)
            {
                var outlineComponent = FindComponent<Outline>(components);

                if (outlineComponent != null)
                {
                    outlineComponent.effectColor = info.outlineColor;
                }

                if (textMeshProComponent != null)
                {
                    textMeshProComponent.outlineColor = info.outlineColor;
                }
            }

            if (info.hasShadow)
            {
                var shadowComponent = FindComponent<Shadow>(components);

                if (shadowComponent != null)
                {
                    shadowComponent.effectColor = info.shadowColor;
                }
            }
        }

        public TextColorInfo GetColorInfo(string colorName)
        {
            if (setting == null) { return null; }
            
            return setting.ColorInfos.FirstOrDefault(x => x.name == colorName);
        }

        private T FindComponent<T>(Component[] components)
        {
            return components.Where(x => x.GetType() == typeof(T)).Cast<T>().FirstOrDefault();
        }
    }
}
