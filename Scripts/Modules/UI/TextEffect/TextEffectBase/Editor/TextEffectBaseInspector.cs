
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.UI.TextEffect
{
    [CustomEditor(typeof(TextEffectBase))]
    public abstract class TextEffectBaseInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private Vector2 scrollPosition = Vector2.zero;

        protected bool update = false;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            EditorApplication.update += ApplyCallback;
        }

        void OnDisable()
        {
            EditorApplication.update -= ApplyCallback;
        }

        private void ApplyCallback()
        {
            var instance = target as TextEffectBase;

            if (instance == null) { return; }

            if (!update) { return; }

            Reflection.InvokePrivateMethod(instance, "Apply");

            update = false;
        }

        protected void DrawMaterialSelector<T>(T instance) where T : TextEffectBase
        {
            var textEffectBase = instance as TextEffectBase;

            var reference = Reflection.GetPrivateField<TextEffectBase, Dictionary<Material, List<TextEffectBase>>>(textEffectBase, "reference", BindingFlags.Static);
            
            if (reference == null || reference.IsEmpty()) { return; }

            var targets = reference.Values
                    .Select(x => x.Select(y => y as T).Where(y => y != null))
                    .Select(x => x.FirstOrDefault())
                    .Where(x => x != null)
                    .Cast<TextEffectBase>()
                    .ToArray();

            // 1つしかない時は自身のみなので不要.
            if (targets.Length <= 1) { return; }

            GUILayout.Space(2f);

            if (EditorLayoutTools.DrawHeader("MaterialSelector", "TextEffectBaseInspector.MaterialSelector"))
            {
                using (new ContentsScope())
                {
                    if (4 <= targets.Length)
                    {
                        using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.Height(130f)))
                        {
                            DrawSelectorContents(targets);

                            scrollPosition = scrollViewScope.scrollPosition;
                        }
                    }
                    else
                    {
                        DrawSelectorContents(targets);
                    }
                }
            }
        }

        protected abstract void DrawSelectorContents(TextEffectBase[] targets);
    }
}
