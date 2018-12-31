
using UnityEngine;
using UnityEngine.UI;
using Unity.Linq;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using SoftMasking;

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

        [NonSerialized]
        private bool initialized = false;

        private static Dictionary<Material, List<TextEffectBase>> reference = null;
        private static Dictionary<string, Material> cache = null;
        
        //----- property -----

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

        #if UNITY_EDITOR
        
        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            Rebuild();
        }

        #endif

        private void Initialize()
        {
            Rebuild();

            if (initialized) { return; }

            if (target == null)
            {
                target = UnityUtility.GetComponent<Text>(gameObject);
            }

            initialized = true;
        }

        private static void Rebuild()
        {
            if (reference == null || cache == null)
            {
                reference = new Dictionary<Material, List<TextEffectBase>>();
                cache = new Dictionary<string, Material>();

                var textEffects = UnityUtility.FindObjectsOfType<TextEffectBase>();

                foreach (var textEffect in textEffects)
                {
                    textEffect.Apply();
                }
            }
        }

        protected void Apply()
        {
            Initialize();

            if (target == null) { return; }

            Release();

            var material = GetMaterial();

            if (material != null)
            {
                if (!reference.ContainsKey(material))
                {
                    reference.Add(material, new List<TextEffectBase>());
                }

                reference[material].Add(this);

                target.material = material;
            }
        }

        protected void Release()
        {
            Initialize();

            var material = target.material;
            
            target.material = null;

            if (reference.ContainsKey(material))
            {
                reference[material].Remove(this);

                if (reference[material].IsEmpty())
                {
                    var item = cache.FirstOrDefault(x => x.Value == material);

                    reference.Remove(material);

                    cache.Remove(item.Key);

                    UnityUtility.SafeDelete(material);
                }
            }
        }

        private Material GetMaterial()
        {
            var softMaskable = IsSoftMaskable();

            var key = GetCacheKey(softMaskable);

            var material = cache.GetValueOrDefault(key);

            if (UnityUtility.IsNull(material))
            {
                cache.Remove(key);
            }
            else
            {
                return material;
            }

            var shader = GetShader(softMaskable);

            if (shader == null) { return null; }

            material = new Material(shader);

            SetShaderParams(material);

            material.hideFlags = HideFlags.DontSave;

            cache[key] = material;

            return material;
        }

        protected bool IsSoftMaskable()
        {
            return !gameObject.Ancestors().OfComponent<SoftMask>().IsEmpty();
        }

        protected abstract void SetShaderParams(Material material);

        protected abstract Shader GetShader(bool softMaskable);

        protected abstract string GetCacheKey(bool softMaskable);
    }
}
