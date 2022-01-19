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
    public sealed class GameTextSetterInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private string currentCategoryGuid = null;

        private string currentTextGuid = null;

        private GameTextSetter instance = null;

        private bool setup = false;
        
        //----- property -----

        public GameTextSetter Instance { get { return instance; } }

        public string SelectionCategoryGuid { get { return currentCategoryGuid; } }

        public static GameTextSetterInspector Current { get; private set; }

        //----- method -----

        private void Setup()
        {
            if (setup) { return; }

            var gameText = GameText.Instance;

            currentCategoryGuid = GetCategoryGuid(gameText, instance.TextGuid);

            currentTextGuid = instance.TextGuid;

            setup = true;
        }

        void OnEnable()
        {
            if (!GameTextLoader.IsLoaded)
            {
                GameTextLoader.Reload();
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
            instance = target as GameTextSetter;
            
            Current = this;

            Setup();

            var gameText = GameText.Instance;
            
            UpdateCurrentInfo(gameText);

            DrawSourceSelectGUI(gameText);

            DrawEmbeddedTextSelectGUI(gameText);

            DrawDistributionTextSelectGUI(gameText);

            DrawDevelopmentTextSelectGUI();
        }

        private void DrawSourceSelectGUI(GameText gameText)
        {
            var contentType = instance.ContentType;

            var config = GameTextConfig.Instance;

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
                            UnityEditorUtility.RegisterUndo("GameTextSetterInspector-Undo", instance);

                            var selection = enumValues.ElementAtOrDefault(index);

                            Reflection.SetPrivateField(instance, "type", selection);

                            SetTextGuid(null);

                            currentCategoryGuid = GetCategoryGuid(gameText, instance.TextGuid);
                        }

                        GUILayout.Space(4f);
                    }
                }

                GUILayout.Space(2f);
            }
        }

        private void DrawEmbeddedTextSelectGUI(GameText gameText)
        {
            var contentType = instance.ContentType;

            if (contentType != ContentType.Embedded){ return; }
            
            DrawTextSelectGUI(gameText, contentType);
        }

        private void DrawDistributionTextSelectGUI(GameText gameText)
        {
            var contentType = instance.ContentType;

            if (contentType != ContentType.Distribution){ return; }
            
            DrawTextSelectGUI(gameText, contentType);
        }

        private void DrawTextSelectGUI(GameText gameText, ContentType contentType)
        {
            var categoryChanged = false;

            using (new EditorGUILayout.HorizontalScope())
            {
                var categories = gameText.Categories.Where(x => x.ContentType == contentType).ToArray();

                // Noneが入るので1ずれる.
                var categoryIndex = categories.IndexOf(x => x.Guid == SelectionCategoryGuid) + 1;

                var categoryLabels = categories.Select(x => x.DisplayName).ToArray();

                var labels = new List<string> { "None" };

                labels.AddRange(categoryLabels);

                EditorGUI.BeginChangeCheck();

                categoryIndex = EditorGUILayout.Popup("GameText", categoryIndex, labels.ToArray());

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("GameTextSetterInspector-Undo", instance);
                    
                    var newCategory = 1 <= categoryIndex ? categories[categoryIndex - 1] : null;

                    var newCategoryGuid = newCategory != null ? newCategory.Guid : string.Empty;

                    if (currentCategoryGuid != newCategoryGuid)
                    {
                        SetTextGuid(null);

                        UpdateCurrentInfo(gameText);

                        currentCategoryGuid = newCategoryGuid;

                        categoryChanged = true;
                    }
                }

                using (new DisableScope(categoryIndex == 0 || GameText.Instance.Texts == null))
                {
                    GUILayout.Space(2f);

                    if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(75f)))
                    {
                        GameTextSelector.Open();
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
                    editText = developmentText.TrimStart(GameTextSetter.DevelopmentMark);
                }

                var prevText = editText;

                EditorGUI.BeginChangeCheck();

                var lineCount = editText.Count(x => x == '\n') + 1;

                lineCount = Mathf.Clamp(lineCount, 1, 5);

                var textAreaHeight = lineCount * 18f;

                editText = EditorGUILayout.TextArea(editText, GUILayout.ExpandWidth(true), GUILayout.Height(textAreaHeight));

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo("GameTextSetterInspector-Undo", instance);

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

        private void UpdateCurrentInfo(GameText gameText)
        {
            // TextGuidが変化していたらカテゴリGuidを更新.
            if (currentTextGuid == instance.TextGuid) { return; }

            currentTextGuid = instance.TextGuid;

            currentCategoryGuid = GetCategoryGuid(gameText, instance.TextGuid);
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

        private string GetCategoryGuid(GameText gameText, string textGuid)
        {
            if (string.IsNullOrEmpty(textGuid)) { return null; }

            var contents = gameText.Texts  as Dictionary<string, TextInfo>;

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
