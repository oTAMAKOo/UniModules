﻿﻿﻿
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.UI.Extension
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Dropdown))]
    public abstract class UIDropdown : UIComponent<Dropdown>
    {
        //----- params -----

        //----- field -----

        private Subject<int> onChange = null;

        //----- property -----

        public Dropdown Dropdown { get { return component; } }

        //----- method -----

        protected override void OnEnable()
        {
            base.OnEnable();

            Dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
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
