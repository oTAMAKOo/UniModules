﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.UI.Element;

namespace Modules.UI.Reactive
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(UIText))]
    public sealed class ButtonReactiveText : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        private UIButton target = null;

        [SerializeField, HideInInspector]
        private Color enableColor = Color.white;
        [SerializeField, HideInInspector]
        private Color disableColor = Color.white;

        [SerializeField, HideInInspector]
        private bool useShadow = false;
        [SerializeField, HideInInspector]
        private Color enableShadowColor = Color.white;
        [SerializeField, HideInInspector]
        private Color disableShadowColor = Color.white;

        [SerializeField, HideInInspector]
        private bool useOutline = false;
        [SerializeField, HideInInspector]
        private Color enableOutlineColor = Color.white;
        [SerializeField, HideInInspector]
        private Color disableOutlineColor = Color.white;

        private UIText uiText = null;

        //----- property -----

        //----- method -----

        void Awake()
        {
            uiText = UnityUtility.GetComponent<UIText>(gameObject);

            if (target != null && Application.isPlaying)
            {
                target.ObserveEveryValueChanged(x => x.Button.interactable).Subscribe(x => Apply(x)).AddTo(this);
            }
        }

        void OnEnable()
        {
            if (target != null)
            {
                Apply(target.Button.interactable);
            }

            // UITextが色指定を行っている場合は現在の色をその色に変更.
            var selectionColor = uiText.SelectionColor;

            if (selectionColor != null)
            {
                enableColor = selectionColor.Color;

                if (selectionColor.ShadowColor.HasValue)
                {
                    enableShadowColor = selectionColor.ShadowColor.Value;
                }

                if (selectionColor.OutlineColor.HasValue)
                {
                    enableOutlineColor = selectionColor.OutlineColor.Value;
                }

                uiText.SetColor(null);
            }
        }

        private void Apply(bool interactable)
        {
            uiText.Text.color = interactable ? enableColor : disableColor;

            if (useShadow)
            {
                if (uiText.Shadow != null)
                {
                    uiText.Shadow.SetColor(interactable ? enableShadowColor : disableShadowColor);
                }

                if (uiText.RichShadow != null)
                {
                    uiText.RichShadow.effectColor = interactable ? enableShadowColor : disableShadowColor;
                }
            }

            if (useOutline)
            {
                if (uiText.Outline != null)
                {
                    uiText.Outline.SetColor(interactable ? enableOutlineColor : disableOutlineColor);
                }

                if (uiText.RichOutline != null)
                {
                    uiText.RichOutline.effectColor = interactable ? enableOutlineColor : disableOutlineColor;
                }
            }
        }
    }
}
