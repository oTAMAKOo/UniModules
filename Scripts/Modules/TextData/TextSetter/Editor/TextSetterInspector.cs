
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
            
            UpdateCurrentInfo();

            DrawSourceSelectGUI(textData);

            DrawEmbeddedTextSelectGUI(textData);

            GUILayout.Space(2f);

            DrawDistributionTextSelectGUI(textData);

            DrawDummyTextSelectGUI();
        }

        private void DrawSourceSelectGUI(TextData textData)
        {
            var type = instance.Type;

            var config = TextDataConfig.Instance;

            using (new DisableScope(!string.IsNullOrEmpty(currentTextGuid)))
            {
                if (config.EnableExternal)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField("Source", GUILayout.Width(45f));

                        GUILayout.FlexibleSpace();

                        var enumValues = Enum.GetValues(typeof(TextType)).Cast<TextType>().ToArray();

                        var index = enumValues.IndexOf(x => x == type);

                        var tabItems = enumValues.Select(x => x.ToString()).ToArray();
                        
                        EditorGUI.BeginChangeCheck();

                        index = GUILayout.Toolbar(index, tabItems, "MiniButton", GUI.ToolbarButtonSize.Fixed, GUILayout.MinWidth(200f));

                        if (EditorGUI.EndChangeCheck())
                        {
                            UnityEditorUtility.RegisterUndo(instance);

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
            var type = instance.Type;

            if (type != TextType.Internal){ return; }
            
            DrawTextSelectGUI();
        }

        private void DrawDistributionTextSelectGUI(TextData textData)
        {
            var type = instance.Type;

            if (type != TextType.External){ return; }
            
            DrawTextSelectGUI();
        }

        private void DrawTextSelectGUI()
        {
            var textData = TextData.Instance;

            if (textData == null){ return; }

            var label = string.Empty;

            var category = textData.Categories.FirstOrDefault(x => x.Guid == currentCategoryGuid);
            var enumName = textData.GetEnumName(currentTextGuid);

            if (category != null && !string.IsNullOrEmpty(enumName))
            {
                label = $"{category.DisplayName} : {enumName}";
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (string.IsNullOrEmpty(label))
                {
                    EditorGUILayout.PrefixLabel("Text");
                }
                else
                {
                    EditorGUILayout.SelectableLabel(label, EditorStyles.textField, GUILayout.Height(EditorLayoutTools.SingleLineHeight));
                }

                using (new DisableScope(TextData.Instance.Texts == null))
                {
                    GUILayout.Space(2f);

                    var layoutOptions = string.IsNullOrEmpty(currentTextGuid) ? 
                                        new GUILayoutOption[0] : 
                                        new GUILayoutOption[]{ GUILayout.Width(60f) };

                    if (GUILayout.Button("select", EditorStyles.miniButton, layoutOptions))
                    {
                        SelectorWindow.Open();
                    }
                }

                if (!string.IsNullOrEmpty(currentTextGuid))
                {
                    GUILayout.Space(2f);

                    if (GUILayout.Button("clear", EditorStyles.miniButton, GUILayout.Width(60f)))
                    {
                        SetTextGuid(null);
                    }
                }
            }
        }

        private void DrawDummyTextSelectGUI()
        {
            var enableDummyText = string.IsNullOrEmpty(currentTextGuid) && string.IsNullOrEmpty(currentCategoryGuid);

            if (!enableDummyText) { return; }
            
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("DummyText");

                var editText = string.Empty;

                var dummyText = (string)Reflection.InvokePrivateMethod(instance, "GetDummyText");

                if (!string.IsNullOrEmpty(dummyText))
                {
                    editText = dummyText.TrimStart(TextSetter.DummyMark);
                }

                var prevText = editText;

                EditorGUI.BeginChangeCheck();

                var lineCount = editText.Count(x => x == '\n') + 1;

                lineCount = Mathf.Clamp(lineCount, 1, 5);

                var textAreaHeight = lineCount * 18f;

                editText = EditorGUILayout.TextArea(editText, GUILayout.ExpandWidth(true), GUILayout.Height(textAreaHeight));

                if (EditorGUI.EndChangeCheck())
                {
                    UnityEditorUtility.RegisterUndo(instance);

                    if (!string.IsNullOrEmpty(prevText))
                    {
                        Reflection.InvokePrivateMethod(instance, "ApplyText", new object[] { null });
                    }

                    if (!string.IsNullOrEmpty(editText))
                    {
                        editText = editText.FixLineEnd();
                    }

                    SetDummyText(editText);
                }
            }
        }

        public void SetTextGuid(string textGuid)
        {
            Reflection.InvokePrivateMethod(instance, "SetTextGuid", new object[] { textGuid });

            SetDummyText(null);

            UpdateCurrentInfo();
        }

        public void SetDummyText(string text)
        {
            Reflection.InvokePrivateMethod(instance, "SetDummyText", new object[] { text });
        }

        private void UpdateCurrentInfo()
        {
            var textData = TextData.Instance;

            // TextGuidが変化していたらカテゴリGuidを更新.
            if (currentTextGuid == instance.TextGuid) { return; }

            currentTextGuid = instance.TextGuid;

            currentCategoryGuid = GetCategoryGuid(textData, instance.TextGuid);
        }

        private void OnUndoRedo()
        {
            if (instance != null)
            {
                Reflection.InvokePrivateMethod(instance, "ImportText");
            }
        }

        public static string GetCategoryGuid(TextData textData, string textGuid)
        {
            if (string.IsNullOrEmpty(textGuid)) { return null; }

            var contents = textData.Texts as Dictionary<string, TextInfo>;

            if (contents == null) { return null; }

            var content = contents.GetValueOrDefault(textGuid);

            if (content == null) { return null; }

            return content.categoryGuid;
        }
    }
}
