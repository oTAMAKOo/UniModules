
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.UI.Extension;
using UnityEngine.UI;

namespace Modules.UI.Reactive
{
    [ExecuteAlways]
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
		}

        private void Apply(bool interactive)
        {
            uiText.Text.color = interactive ? enableColor : disableColor;

            var components = UnityUtility.GetComponents<Component>(gameObject).ToArray();

            if (useShadow)
            {
                var shadowComponent = FindComponent<Shadow>(components);

                if (shadowComponent != null)
                {
                    shadowComponent.effectColor = interactive ? enableShadowColor : disableShadowColor;
                }
            }

            if (useOutline)
            {
                var outlineComponent = FindComponent<Outline>(components);

                if (outlineComponent != null)
                {
                    outlineComponent.effectColor = interactive ? enableOutlineColor : disableOutlineColor;
                }
            }
        }

        private T FindComponent<T>(IEnumerable<Component> components)
        {
            return components.Where(x => x != null)
                .Where(x => x.GetType() == typeof(T))
                .Cast<T>()
                .FirstOrDefault();
        }
    }
}
