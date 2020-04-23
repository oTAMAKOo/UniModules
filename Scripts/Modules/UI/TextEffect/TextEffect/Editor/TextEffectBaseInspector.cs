
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
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

        //----- property -----

        //----- method -----
        
        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        protected void Apply()
        {
            var instance = target as TextEffectBase;

            if (instance == null) { return; }
            
            TextEffectManager.Instance.Apply(instance);
        }

        protected void DrawMaterialSelector<T>(T instance) where T : TextEffectBase
        {
            var reference = Reflection.GetPrivateField<TextEffectManager, Dictionary<Material, List<TextEffectBase>>>(TextEffectManager.Instance, "reference");
            
            if (reference == null || reference.IsEmpty()) { return; }

            var targets = reference.Values
                    .Select(x => x.OfType<T>().FirstOrDefault())
                    .Where(x => x != null)
                    .Cast<TextEffectBase>()
                    .Distinct()
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

        private void OnUndoRedo()
        {
            var instance = target as TextEffectBase;

            if (instance == null) { return; }

            Apply();
        }

        protected abstract void DrawSelectorContents(TextEffectBase[] targets);
    }
}
