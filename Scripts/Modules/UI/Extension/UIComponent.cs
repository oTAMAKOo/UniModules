﻿﻿
using System;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using Extensions;

namespace Modules.UI.Extension
{
    [ExecuteInEditMode]
    public abstract class UIComponentBehaviour : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        protected virtual void OnEnable()
        {
            Modify();
        }

        private void OnRectTransformParentChanged()
        {
            Modify();
        }

        protected virtual void Modify() { }
    }

    public abstract class UIComponent<T> : UIComponentBehaviour where T : Component
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
    }
}
