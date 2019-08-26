﻿﻿
using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Extensions;

namespace Modules.UI.Element
{
    [ExecuteInEditMode]
    public abstract class UIElement<T> : MonoBehaviour where T : Component
    {
        //----- params -----

        //----- field -----

        private T targetComponent = null;

        //----- property -----

        public T component
        {
            get
            {
                if (targetComponent == null)
                {
                    targetComponent = UnityUtility.GetComponent<T>(gameObject);
                }

                return targetComponent;
            }
        }

        //----- method -----

        protected virtual void OnEnable()
        {
            Modify();
        }

        private void OnRectTransformParentChanged()
        {
            Modify();
        }

        public abstract void Modify();
    }
}