﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.TextData.Editor;

namespace Modules.TextData.Components
{
    [CustomEditor(typeof(TextSetter))]
    public sealed class TextSetterInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private string currentCategoryGuid = null;

        private string currentTextGuid = null;

        private TextSetter instance = null;

        private bool setup = false;
        
        //----- property -----

        public TextSetter Instance { get { return instance; } }

        public string SelectionCategoryGuid { get { return currentCategoryGuid; } }

        public static TextSetterInspector Current { get; private set; }

        //----- method -----

        private void Setup()
        {
            if (setup) { return; }

            var textData = TextData.Instance;

            currentCategoryGuid = GetCategoryGuid(textData, instance.TextGuid);

            currentTextGuid = instance.TextGuid;

            setup = true;
        }

        void OnEnable()
        {
            if (!TextDataLoader.IsLoaded)
            {
                TextDataLoader.Reload();
            }

            Undo.undoRedoPerformed += OnUndoRedo;

            setup = false;
        }

        void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Current = null;
        }

        public override void OnInspectorGUI()
        {
            instance = target as TextSetter;
            
            Current = this;

            Setup();

            var textData = TextData.Instance;
            
            UpdateCurrentInfo(textData);

            DrawSourceSelectGUI(textData);

            DrawEmbeddedTextSelectGUI(textData);

            DrawDistributionTextSelectGUI(textData);

            DrawDevelopmentTextSelectGUI();
        }

        private void DrawSourceSelectGUI(TextData textData)
        {
            var contentType = instance.ContentType;

            var config = TextDataConfig.Instance;

            var distributionSetting = config.Distribution;

            using (new DisableScope(!string.IsNullOrEmpty(currentTextGuid)))
            {
                if (distributionSetting.Enable)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Source", GUILayout.Width(45f));

                        GUILayout.FlexibleSpace();

                        var enumValues = Enum.GetValues(typeof(ContentType)).Cast<ContentType>().ToArray();

                        var index = enumValues.IndexOf(x => x == contentType);

                        var tabItems = enumValues.Select(x => x.ToString()).ToArray();
                        
                        EditorGUI.BeginChangeCheck();

                        index = GUILayout.Toolbar(index, tabItems, "MiniButton", GUI.ToolbarButtonSize.Fixed, GUILayout.MinWidth(200f));

                        if (EditorGUI.EndChangeCheck())
                        {
                            UnityEditorUtility.RegisterUndo("TextDataSetterInspector-Undo", instance);

                            var selection = enumValues.ElementAtOrDefault(index);

                            Reflection.SetPrivateField(instance, "type", selection);

                            SetTextGuid(null);

                            currentCategoryGuid = GetCategoryGuid(textData, instance.TextGuid);
                        }

                        GUILayout.Space(4f);
                    }
                }

                GUILayout.Space(2f);
            }
        }

        private void DrawEmbeddedTextSelectGUI(TextData textData)
        {
            var contentType = instance.ContentType;

            if (contentType != ContentType.Embedded){ return; }
            
            DrawTextSelectGUI(textData, contentType);
        }

        private void DrawDistributionTextSelectGUI(TextData textData)
        {
            var contentType = instance.ContentType;

            if (contentType != ContentType.Distribution){ return; }
            
            DrawTextSelectGUI(textData, contentType);
        }

        private void DrawTextSelectGUI(TextData textData, ContentType contentType)
        {
            var categoryChanged = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                var categories = textData.Categories.Where(x => x.ContentType == contentType).ToArray();

                // Noneが入るので1ずれる.
                var categoryIndex = categories.IndexOf(x => x.Guid == SelectionCategoryGuid) + 1;

                var categoryLabels = categories.Select(x => x.DisplayName).ToArray();

                var labels = new List<string> { "None" };

                labels.AddRange(categoryLabels);

                EditorGUI.BeginChangeCheck();

                categoryIndex = EditorGUILayout.Popup("TextData", categoryIndex, labels.ToArray());

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("TextDataSetterInspector-Undo", instance);
                    
                    var newCategory = 1 <= categoryIndex ? categories[categoryIndex - 1] : null;

                    var newCategoryGuid = newCategory != null ? newCategory.Guid : string.Empty;

                    if (currentCategoryGuid != newCategoryGuid)
                    {
                        SetTextGuid(null);

                        UpdateCurrentInfo(textData);

                        currentCategoryGuid = newCategoryGuid;

                        categoryChanged = true;
                    }
                }

                using (new DisableScope(categoryIndex == 0 || TextData.Instance.Texts == null))
                {
                    GUILayout.Space(2f);

                    if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(75f)))
                    {
                        TextDataSelector.Open();
                    }
                }
            }

            if (categoryChanged && string.IsNullOrEmpty(currentCategoryGuid))
            {
                SetDevelopmentText(string.Empty);
            }
        }

        private void DrawDevelopmentTextSelectGUI()
        {
            var enableDevelopmentText = string.IsNullOrEmpty(currentTextGuid) && string.IsNullOrEmpty(currentCategoryGuid);

            if (!enableDevelopmentText) { return; }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                var labelWidth = EditorGUIUtility.labelWidth - 10f;

                EditorGUILayout.LabelField("Development Text", GUILayout.Width(labelWidth));

                var editText = string.Empty;

                var developmentText = (string)Reflection.InvokePrivateMethod(instance, "GetDevelopmentText");

                if (!string.IsNullOrEmpty(developmentText))
                {
                    editText = developmentText.TrimStart(TextSetter.DevelopmentMark);
                }

                var prevText = editText;

                EditorGUI.BeginChangeCheck();

                var lineCount = editText.Count(x => x == '\n') + 1;

                lineCount = Mathf.Clamp(lineCount, 1, 5);

                var textAreaHeight = lineCount * 18f;

                editText = EditorGUILayout.TextArea(editText, GUILayout.ExpandWidth(true), GUILayout.Height(textAreaHeight));

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("TextDataSetterInspector-Undo", instance);

                    if (!string.IsNullOrEmpty(prevText))
                    {
                        Reflection.InvokePrivateMethod(instance, "ApplyText", new object[] { null });
                    }

                    if (!string.IsNullOrEmpty(editText))
                    {
                        editText = editText.FixLineEnd();
                    }

                    SetDevelopmentText(editText);
                }
            }
        }

        private void UpdateCurrentInfo(TextData textData)
        {
            // TextGuidが変化していたらカテゴリGuidを更新.
            if (currentTextGuid == instance.TextGuid) { return; }

            currentTextGuid = instance.TextGuid;

            currentCategoryGuid = GetCategoryGuid(textData, instance.TextGuid);
        }

        private void SetTextGuid(string textGuid)
        {
            Reflection.InvokePrivateMethod(instance, "SetTextGuid", new object[] { textGuid });

            SetDevelopmentText(null);
        }

        private void SetDevelopmentText(string text)
        {
            Reflection.InvokePrivateMethod(instance, "SetDevelopmentText", new object[] { text });
        }

        private string GetCategoryGuid(TextData textData, string textGuid)
        {
            if (string.IsNullOrEmpty(textGuid)) { return null; }

            var contents = textData.Texts  as Dictionary<string, TextInfo>;

            if (contents == null) { return null; }

            var content = contents.GetValueOrDefault(textGuid);

            if (content == null) { return null; }

            return content.categoryGuid;
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
