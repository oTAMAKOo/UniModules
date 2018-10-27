
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.Callbacks;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

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

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            Rebuild();
        }

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

                Debug.LogFormat("Add Material : cache count = {0}", cache.Count);
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

                    Debug.LogFormat("Release Material : cache count = {0}", cache.Count);
                }
            }
        }

        private Material GetMaterial()
        {
            var key = GetCacheKey();

            var material = cache.GetValueOrDefault(key);

            if (UnityUtility.IsNull(material))
            {
                cache.Remove(key);
            }
            else
            {
                return material;
            }

            var shader = GetShader();

            if (shader == null) { return null; }

            material = new Material(shader);

            SetShaderParams(material);

            material.hideFlags = HideFlags.DontSave;

            cache[key] = material;

            return material;
        }

        protected abstract Shader GetShader();

        protected abstract void SetShaderParams(Material material);

        protected abstract string GetCacheKey();
    }
}
