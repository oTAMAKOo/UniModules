﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.GameText.Editor;

namespace Modules.GameText.Components
{
    [CustomEditor(typeof(GameTextSetter))]
    public class GameTextSetterInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private GameTextCategory? gameTextCategory = null;

        private GameTextSetter instance = null;

        //----- property -----

        public GameTextSetter Instance { get { return instance; } }

        public static GameTextSetterInspector Current { get; private set; }

        //----- method -----

        void OnEnable()
        {
            if (!GameTextLoader.IsLoaded)
            {
                GameTextLoader.Reload();
            }

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Current = null;
        }

        public override void OnInspectorGUI()
        {
            instance = target as GameTextSetter;

            Current = this;

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                var categories = Enum.GetValues(typeof(GameTextCategory)).Cast<GameTextCategory>().OrderBy(x => (int)x).ToList();

                var categoryIndex = categories.FindIndex(x => x == instance.Category);

                var categoryLabels = categories.Select(x => x.ToLabelName());
                var labels = categoryLabels.ToArray();

                var currentCategoryIndex = EditorGUILayout.Popup("GameText", categoryIndex, labels);

                if (categoryIndex != currentCategoryIndex)
                {
                    UnityEditorUtility.RegisterUndo("GameTextSetterInspector-Undo", instance);
                    categoryIndex = currentCategoryIndex;

                    instance.SetCategory(categories[categoryIndex]);
                }

                using (new DisableScope(categoryIndex == 0 || GameText.Instance.Cache == null))
                {
                    GUILayout.Space(2f);
                    
                    if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(75f)))
                    {
                        GameTextSelector.Open();
                    }
                }
            }

            GUILayout.Space(2f);

            if (gameTextCategory.HasValue)
            {
                if (gameTextCategory.Value != instance.Category)
                {
                    OnGameTextSetterCategoryChanged(instance.Category);
                }
            }
            else
            {
                OnGameTextSetterCategoryChanged(instance.Category);
            }

            gameTextCategory = instance.Category;

            if (instance.Category == GameTextCategory.None)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var labelWidth = EditorGUIUtility.labelWidth - 10f;

                    EditorGUILayout.LabelField("Development Text", GUILayout.Width(labelWidth));

                    var developmentText = instance.GetDevelopmentText();

                    var editText = developmentText.TrimStart(GameTextSetter.DevelopmentMark);

                    var prevText = editText;

                    EditorGUI.BeginChangeCheck();

                    var lineCount = editText.Count(x => x == '\n') + 1;

                    lineCount = Mathf.Clamp(lineCount, 1, 5);

                    var textAreaHeight = lineCount * 18f;

                    editText = EditorGUILayout.TextArea(editText, GUILayout.ExpandWidth(true), GUILayout.Height(textAreaHeight));

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("UITextInspector-Undo", instance);

                        if (!string.IsNullOrEmpty(prevText))
                        {
                            Reflection.InvokePrivateMethod(instance, "ApplyText", new object[] { null });
                        }

                        if (!string.IsNullOrEmpty(editText))
                        {
                            editText = editText.FixLineEnd();
                        }

                        Reflection.InvokePrivateMethod(instance, "SetDevelopmentText", new object[] { editText });
                        instance.ImportText();
                    }
                }
            }
        }

        private void OnGameTextSetterCategoryChanged(GameTextCategory category)
        {
            if (category != GameTextCategory.None)
            {
                Reflection.InvokePrivateMethod(instance, "SetDevelopmentText", new object[] { null });               
            }

            instance.ImportText();
        }

        private void OnUndoRedo()
        {
            if (instance != null)
            {
                instance.ImportText();
            }
        }
    }
}
