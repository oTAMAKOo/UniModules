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

        private GameTextSetter instance = null;

        private bool setup = false;
        
        //----- property -----

        public GameTextSetter Instance { get { return instance; } }

        public string SelectionCategoryGuid { get; private set; }

        public static GameTextSetterInspector Current { get; private set; }

        //----- method -----

        private void Setup()
        {
            if (setup) { return; }

            var gameText = GameText.Instance;

            SelectionCategoryGuid = GetCurrentCategoryGuid(gameText);

            setup = true;
        }

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

            Setup();

            GUILayout.Space(5f);

            var gameText = GameText.Instance;

            var currentCategoryGuid = GetCurrentCategoryGuid(gameText);

            var categoryChanged = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                var categoryType = (Type)Reflection.InvokePrivateMethod(gameText, "GetCategoriesType");

                var categories = Enum.GetValues(categoryType).Cast<Enum>().ToList();

                var categoryIndex = 0;

                for (var i = 0; i < categories.Count; i++)
                {
                    var categoryGuid = FindCategoryGuid(gameText, categories[i]);

                    if (categoryGuid == SelectionCategoryGuid)
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

                    var newCategory = 1 <= categoryIndex ? categories[categoryIndex - 1] : null;

                    var newCategoryGuid = FindCategoryGuid(gameText, newCategory);

                    if (currentCategoryGuid != newCategoryGuid)
                    {
                        SelectionCategoryGuid = newCategoryGuid;

                        Reflection.InvokePrivateMethod(instance, "SetText", new object[] { null });

                        categoryChanged = true;
                    }
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

            if (categoryChanged)
            {
                OnGameTextSetterCategoryChanged(SelectionCategoryGuid);
            }

            if (string.IsNullOrEmpty(SelectionCategoryGuid))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    var labelWidth = EditorGUIUtility.labelWidth - 10f;

                    EditorGUILayout.LabelField("Development Text", GUILayout.Width(labelWidth));

                    var developmentText = (string)Reflection.InvokePrivateMethod(instance, "GetDevelopmentText");

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
                    }
                }
            }
        }

        private void OnGameTextSetterCategoryChanged(string categoryGuid)
        {
            if (string.IsNullOrEmpty(categoryGuid))
            {
                Reflection.InvokePrivateMethod(instance, "SetDevelopmentText", new object[] { string.Empty });
            }
        }

        private string GetCurrentCategoryGuid(GameText gameText)
        {
            var categoryEnum = (Enum)Reflection.InvokePrivateMethod(gameText, "FindCategoryEnumFromTextGuid", new object[] { instance.TextGuid });
            
            return FindCategoryGuid(gameText, categoryEnum);
        }

        private string FindCategoryGuid(GameText gameText, Enum categoryEnum)
        {
            if (categoryEnum == null) { return null; }

            return (string)Reflection.InvokePrivateMethod(gameText, "FindCategoryGuid", new object[] { categoryEnum });
        }

        private void OnUndoRedo()
        {
            if (instance != null)
            {
                Reflection.InvokePrivateMethod(instance, "ImportText");
            }
        }
    }
}
