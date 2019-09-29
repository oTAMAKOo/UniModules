
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

#if ENABLE_SOFT_MASK

using SoftMasking;

#endif

#if UNITY_EDITOR

using UnityEditor.Callbacks;

#endif

namespace Modules.UI.TextEffect
{
    [ExecuteInEditMode]
    public abstract class TextEffectBase : MonoBehaviour
    {
        //----- params -----

        //----- field -----

        private bool alive = true;

        private Text target = null;

        //----- property -----

        public bool Alive { get { return alive; } }

        public Text Text
        {
            get { return target ?? (target = UnityUtility.GetComponent<Text>(gameObject)); }
        }

        //----- method -----
                
        void OnEnable()
        {
            TextEffectManager.Instance.Apply(this);
        }

        void OnDestroy()
        {
            alive = false;

            TextEffectManager.Instance.Apply(this);
        }

        public abstract void SetShaderParams(Material material);

        public abstract string GetCacheKey();
    }
}
