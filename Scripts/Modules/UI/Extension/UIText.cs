
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using Extensions;
using Extensions.Serialize;
using Modules.UI.TextEffect;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(Text))]
    public abstract partial class UIText : UIComponent<Text>
    {
        //----- params -----

        public sealed class TextColor
        {
            public int Type { get; private set; }
            public string LabelName { get; private set; }
            public Color Color { get; private set; }
            public Color? ShadowColor { get; private set; }
            public Color? OutlineColor { get; private set; }

            public TextColor()
            {
                Type = -1;
                LabelName = "None";
                Color = Color.clear;
                ShadowColor = null;
                OutlineColor = null;
            }

            public TextColor(Enum type, string labelName, string hexTextColor, string hexShadowColor, string hexOutlineColor)
            {
                Type = Convert.ToInt32(type);
                LabelName = labelName;
                Color = hexTextColor.HexToColor();
                ShadowColor = string.IsNullOrEmpty(hexShadowColor) ? null : (Color?)hexShadowColor.HexToColor();
                OutlineColor = string.IsNullOrEmpty(hexOutlineColor) ? null : (Color?)hexOutlineColor.HexToColor();
            }
        }

        //----- field -----

        [SerializeField, HideInInspector]
        private IntNullable selection = new IntNullable(null);

        //----- property -----

        protected abstract TextColor[] ColorInfos { get; }

        public Text Text { get { return component; } }

        public string text
        {
            get { return component.text; }
            set { component.text = value; }
        }

        public TextColor SelectionColor
        {
            get { return selection.HasValue ? ColorInfos.FirstOrDefault(x => x.Type == selection.Value) : null; }
        }

        //----- method -----

        void OnEnable()
        {
            if (selection.HasValue)
            {
                SetColor(selection);
            }
        }

        public void SetColor(int? type)
        {
            if (type.HasValue)
            {
                var info = ColorInfos.FirstOrDefault(x => x.Type == type.Value);

                if (info != null)
                {
                    selection = Convert.ToInt32(type);

                    if (text != null)
                    {
                        component.color = info.Color;
                    }
                }
                else
                {
                    selection = null;
                }
            }
            else
            {
                selection = null;
            }
        }
    }
}
