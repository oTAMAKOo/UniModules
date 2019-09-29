
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UniRx;
using Extensions;
using Modules.UI.TextEffect;
using Unity.Linq;
#if ENABLE_SOFT_MASK

using SoftMasking;

#endif

#if UNITY_EDITOR

using UnityEditor.Callbacks;

#endif

namespace Modules.UI.TextEffect
{
    public sealed class TextEffectManager : Singleton<TextEffectManager>
    {
        //----- params -----

        //----- field -----

        private Dictionary<Material, List<TextEffectBase>> reference = null;
        private Dictionary<string, Material> materials = null;
        private Dictionary<string, Shader> shaders = null;

        //----- property -----

        //----- method -----

        protected override void OnCreate()
        {
            reference = new Dictionary<Material, List<TextEffectBase>>();
            materials = new Dictionary<string, Material>();
            shaders = new Dictionary<string, Shader>();
        }

        public void Apply(TextEffectBase target)
        {
            Release(target);

            var softMaskable = IsSoftMaskable(target);

            var components = UnityUtility.GetComponents<TextEffectBase>(target.gameObject)
                .Where(x => x.Alive)
                .ToArray();

            var cacheKey = GetCacheKey(components, softMaskable);

            var material = GetMaterial(cacheKey, components, softMaskable);            

            if (material != null)
            {
                if (!reference.ContainsKey(material))
                {
                    reference.Add(material, new List<TextEffectBase>());
                }

                if (target.Alive)
                {
                    reference[material].Add(target);
                }                
            }

            target.Text.material = material;
        }

        private void Release(TextEffectBase target)
        {
            var material = target.Text.material;

            if (!reference.ContainsKey(material)) { return; }

            reference[material].Remove(target);

            if (reference[material].IsEmpty())
            {
                var item = materials.FirstOrDefault(x => x.Value == material);

                reference.Remove(material);

                materials.Remove(item.Key);

                target.Text.material = null;

                UnityUtility.SafeDelete(material);
            }
        }

        private bool IsSoftMaskable(TextEffectBase target)
        {
            #if ENABLE_SOFT_MASK

            return !target.gameObject.Ancestors().OfComponent<SoftMask>().IsEmpty();

            #else

            return false;

            #endif
        }

        private Material GetMaterial(string cacheKey, TextEffectBase[] components, bool softMaskable)
        {
            var material = materials.GetValueOrDefault(cacheKey);

            if (UnityUtility.IsNull(material))
            {
                materials.Remove(cacheKey);
            }
            else
            {
                return material;
            }

            var shader = GetShader(components, softMaskable);

            if (shader == null) { return null; }

            material = new Material(shader);

            material.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;

            components.ForEach(x => x.SetShaderParams(material));

            materials[cacheKey] = material;

            return material;
        }

        private string GetCacheKey(TextEffectBase[] components, bool softMaskable)
        {
            var builder = new StringBuilder();

            foreach (var component in components)
            {
                if (0 < builder.Length)
                {
                    builder.Append("_");
                }

                builder.Append(component.GetCacheKey());
            }

            builder.AppendFormat("_{0}", softMaskable);

            return builder.ToString();
        }

        private Shader GetShader(TextEffectBase[] components, bool softMaskable)
        {
            var shaderName = string.Empty;

            var hasShadow = components.Any(x => x is TextShadow);
            var hasOutline = components.Any(x => x is TextOutline);

            if (hasShadow)
            {
                shaderName = "Custom/UI/Text-Shadow";
            }

            if (hasOutline)
            {
                shaderName = "Custom/UI/Text-Outline";
            }

            if (hasShadow && hasOutline)
            {
                shaderName = "Custom/UI/Text-Outline-Shadow";
            }

            if (string.IsNullOrEmpty(shaderName)) { return null; }

            if (softMaskable)
            {
                shaderName += " (SoftMask)";
            }

            var shader = shaders.GetValueOrDefault(shaderName);

            if (shader == null)
            {
                shader = Shader.Find(shaderName);

                if (shader != null)
                {
                    shaders.Add(shaderName, shader);
                }
                else
                {
                    Debug.LogErrorFormat("Shader not found.\n{0}", shaderName);
                }
            }

            return shader;
        }

        #if UNITY_EDITOR

        [DidReloadScripts]
        private static void DidReloadScripts()
        {
            Instance.Rebuild();
        }

        private void Rebuild()
        {
            reference.Clear();
            materials.Clear();
            shaders.Clear();

            var textEffects = UnityUtility.FindObjectsOfType<TextEffectBase>();

            foreach (var textEffect in textEffects)
            {
                Apply(textEffect);
            }
        }

        #endif
    }
}
