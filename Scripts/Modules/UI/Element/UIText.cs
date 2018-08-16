﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Serialize;

namespace Modules.UI.Element
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Text))]
    public abstract class UIText : UIElement<Text>
    {
        //----- params -----

        public class TextColor
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

        private Shadow shadow = null;
        private DropShadow dropShadow = null;
        private Outline outline = null;
        private RichOutline richOutline = null;

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

        public Shadow Shadow { get { return shadow ?? (shadow = UnityUtility.GetComponent<Shadow>(gameObject)); } }

        public DropShadow DropShadow { get { return dropShadow ?? (dropShadow = UnityUtility.GetComponent<DropShadow>(gameObject)); } }

        public Outline Outline { get { return outline ?? (outline = UnityUtility.GetComponent<Outline>(gameObject)); } }

        public RichOutline RichOutline { get { return richOutline ?? (richOutline = UnityUtility.GetComponent<RichOutline>(gameObject)); } }

        //----- method -----

        public override void Modify()
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

                    //====== Shadow ======

                    if (Shadow != null)
                    {
                        Shadow.enabled = info.ShadowColor.HasValue;

                        if (info.ShadowColor.HasValue)
                        {
                            Shadow.effectColor = info.ShadowColor.Value;
                        }
                    }

                    //====== Drop Shadow ======

                    if (DropShadow != null)
                    {
                        DropShadow.enabled = info.ShadowColor.HasValue;

                        if (info.ShadowColor.HasValue)
                        {
                            DropShadow.effectColor = info.ShadowColor.Value;
                        }
                    }

                    //====== Outline ======

                    if (Outline != null)
                    {
                        Outline.enabled = info.OutlineColor.HasValue;

                        if (info.OutlineColor.HasValue)
                        {
                            Outline.effectColor = info.OutlineColor.Value;
                        }
                    }

                    //====== RichOutline ======

                    if (RichOutline != null)
                    {
                        RichOutline.enabled = info.OutlineColor.HasValue;

                        if (info.OutlineColor.HasValue)
                        {
                            RichOutline.effectColor = info.OutlineColor.Value;
                        }
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