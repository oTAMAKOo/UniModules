
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

        private Text target = null;

        //----- property -----

        public Text Text
        {
            get { return target ?? (target = UnityUtility.GetComponent<Text>(gameObject)); }
        }

        //----- method -----
        
        void OnEnable()
        {
            Apply();
        }

        void OnDisable()
        {
            Release();
        }

        void OnDestroy()
        {
            Release();
        }

        public void Apply()
        {
            TextEffectManager.Instance.Apply(this);
        }

        public void Release()
        {
            TextEffectManager.Instance.Release(this);
        }

        public abstract void SetShaderParams(Material material);

        public abstract string GetCacheKey();
    }
}
