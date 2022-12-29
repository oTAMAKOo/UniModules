
using UnityEngine;
using System;
using UniRx;
using TMPro;

namespace Modules.UI.Extension
{
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Dropdown))]
    public abstract class UIDropdown : UIComponent<TMP_Dropdown>
    {
        //----- params -----

        //----- field -----

        private Subject<int> onChange = null;

        private bool initialized = false;

        //----- property -----

        public TMP_Dropdown Dropdown { get { return component; } }

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
