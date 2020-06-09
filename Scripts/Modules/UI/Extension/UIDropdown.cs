﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;

namespace Modules.UI.Extension
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Dropdown))]
    public abstract class UIDropdown : UIComponent<Dropdown>
    {
        //----- params -----

        //----- field -----

        private Subject<int> onChange = null;

        private bool initialized = false;

        //----- property -----

        public Dropdown Dropdown { get { return component; } }

        //----- method -----

        void OnEnable()
        {
            if (!initialized)
            {
                Dropdown.onValueChanged.AddListener(OnDropdownValueChanged);

                initialized = true;
            }
        }

        private void OnDropdownValueChanged(int index)
        {
            if (onChange != null)
            {
                onChange.OnNext(index);
            }
        }

        public IObservable<int> OnChangeAsObservable()
        {
            return onChange ?? (onChange = new Subject<int>());
        }
    }
}
