﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(Toggle))]
    public abstract class UIToggle : UIComponent<Toggle>
    {
        //----- params -----

        //----- field -----

        private Subject<bool> onChange = null;

        private bool initialized = false;

        //----- property -----

        public Toggle Toggle { get { return component; } }

        public bool isOn
        {
            get { return component.isOn; }
            set { component.isOn = value; }
        }

        //----- method -----

        void OnEnable()
        {
            if (!initialized)
            {
                Toggle.onValueChanged.AddListener(OnToggleValueChanged);

                initialized = true;
            }
        }

        private void OnToggleValueChanged(bool value)
        {
            if (onChange != null)
            {
                onChange.OnNext(value);
            }
        }

        public IObservable<bool> OnChangeAsObservable()
        {
            return onChange ?? (onChange = new Subject<bool>());
        }
    }
}
