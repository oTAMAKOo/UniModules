﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
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

        private string categoryGuid = null;

        private GameTextSetter instance = null;

        //----- property -----

        public GameTextSetter Instance { get { return instance; } }

        public static GameTextSetterInspector Current { get; private set; }

        //----- method -----
        
        void OnEnable()
        {
            instance = target as GameTextSetter;

            categoryGuid = instance.CategoryGuid;

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

            GUILayout.Space(5f);

            var gameText = GameText.Instance;
            
            using (new EditorGUILayout.HorizontalScope())
            {
                var categoryType = gameText.GetCategoriesType();

                var categories = Enum.GetValues(categoryType).Cast<Enum>().ToList();

                var categoryIndex = 0;

                for (var i = 0; i < categories.Count; i++)
                {
                    var categoryGuid = gameText.FindCategoryGuid(categories[i]);

                    if (categoryGuid == instance.CategoryGuid)
                    {
                        // Noneが入るので1ずれる.
                        categoryIndex = i + 1;
                        break;
                    }
                }

                var categoryLabels = categories.Select(x => x.ToLabelName());

                var labels = new List<string> { "None" };

                labels.AddRange(categoryLabels);

                var currentCategoryIndex = EditorGUILayout.Popup("GameText", categoryIndex, labels.ToArray());

                if (categoryIndex != currentCategoryIndex)
                {
                    UnityEditorUtility.RegisterUndo("GameTextSetterInspector-Undo", instance);

                    categoryIndex = currentCategoryIndex;

                    instance.ChangeCategory(1 <= categoryIndex ? categories[categoryIndex - 1] : null);
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

            if (categoryGuid != instance.CategoryGuid)
            {
                OnGameTextSetterCategoryChanged(instance.CategoryGuid);
            }

            categoryGuid = instance.CategoryGuid;

            if (string.IsNullOrEmpty(instance.CategoryGuid))
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

        private void OnGameTextSetterCategoryChanged(string categoryGuid)
        {
            if (string.IsNullOrEmpty(categoryGuid))
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
