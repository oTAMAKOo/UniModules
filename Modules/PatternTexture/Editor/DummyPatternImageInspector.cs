
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using Extensions;
using Extensions.Devkit;

namespace Modules.PatternTexture
{
    [CustomEditor(typeof(DummyPatternImage))]
    public sealed class DummyPatternImageInspector : ScriptlessEditor
    {
        //----- params -----

        //----- field -----

        private DummyPatternImage instance = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndoRedoPerformed;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedoPerformed;
        }

        public override void OnInspectorGUI()
        {
            instance = target as DummyPatternImage;

            GUILayout.Space(4f);

            DrawDefaultScriptlessInspector();

            var assetGuid = Reflection.GetPrivateField<DummyPatternImage, string>(instance, "assetGuid");

            var patternTexture = Reflection.InvokePrivateMethod(instance, "LoadPatternTexture") as PatternTexture;

            var patternName = Reflection.InvokePrivateMethod(instance, "FindPatternName", new object[] { patternTexture }) as string;

            // PatternTexture.

            EditorGUI.BeginChangeCheck();

            var selectionTexture = EditorLayoutTools.ObjectField("PatternTexture", patternTexture, false);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(instance);

                Reflection.InvokePrivateMethod(instance, "SetDummyAsset", new object[] { selectionTexture });

                EditorUtility.SetDirty(instance);
            }

            if (!string.IsNullOrEmpty(assetGuid) && patternTexture == null)
            {
                EditorGUILayout.HelpBox("PatternTexture is not found.", MessageType.Warning);
            }

            if (patternTexture == null) { return; }

            // Pattern.

            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.Width(76f)))
                {
                    GUILayout.Space(2f);

                    if (EditorLayoutTools.PrefixButton("Sprite", GUILayout.Width(76f), GUILayout.Height(18f)))
                    {
                        var selectionInstance = instance;

                        Action<string> onSelection = x =>
                        {
                            UnityEditorUtility.RegisterUndo(selectionInstance);

                            Reflection.InvokePrivateMethod(selectionInstance, "SetDummyPatternName", new object[] { x });

                            EditorUtility.SetDirty(selectionInstance);
                            InternalEditorUtility.RepaintAllViews();
                        };

                        PatternSpriteSelector.Show(patternTexture, patternName, onSelection, null);
                    }
                }

                GUILayout.Space(4f);

                if (!string.IsNullOrEmpty(patternName))
                {
                    EditorGUILayout.SelectableLabel(patternName, EditorStyles.textArea, GUILayout.Height(18f));
                }
            }

            var patternNameHash = Reflection.GetPrivateField<DummyPatternImage, string>(instance, "patternNameHash");

            if (!string.IsNullOrEmpty(patternNameHash) && string.IsNullOrEmpty(patternName))
            {
                EditorGUILayout.HelpBox("Pattern is not found.", MessageType.Warning);
            }
        }

        private void OnUndoRedoPerformed()
        {
            foreach (var item in targets)
            {
                var dummyPatternImage = item as DummyPatternImage;

                if (dummyPatternImage == null) { continue; }

                Reflection.InvokePrivateMethod(dummyPatternImage, "ApplyDummy");
            }
        }
    }
}
