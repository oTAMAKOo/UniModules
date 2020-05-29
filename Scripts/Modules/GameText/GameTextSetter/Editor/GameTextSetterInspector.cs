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

            UpdateSelectionAssetType(gameText, instance.TextGuid);

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

            GUILayout.Space(4f);

            var gameText = GameText.Instance;

            var config = GameTextConfig.Instance;

            // TextGuidが変化していたらカテゴリGuidを更新.
            if (currentTextGuid != instance.TextGuid)
            {
                currentTextGuid = instance.TextGuid;

                currentCategoryGuid = GetCategoryGuid(gameText, instance.TextGuid);
            }

            // モード選択タブ.

            var assetType = GameTextSetter.AssetType.Internal;

            if (config.UseExternalAseet)
            {
                assetType = Reflection.GetPrivateField<GameTextSetter, GameTextSetter.AssetType>(instance, "assetType");

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("Source Asset", GUILayout.Width(45f));

                    GUILayout.FlexibleSpace();

                    var enumValues = Enum.GetValues(typeof(GameTextSetter.AssetType)).Cast<GameTextSetter.AssetType>().ToArray();

                    var index = enumValues.IndexOf(x => x == assetType);

                    var tabItems = enumValues.Select(x => x.ToLabelName()).ToArray();

                    var tabEnable = !string.IsNullOrEmpty(currentCategoryGuid) || string.IsNullOrEmpty(currentTextGuid);

                    using (new DisableScope(!tabEnable))
                    {
                        EditorGUI.BeginChangeCheck();

                        index = GUILayout.Toolbar(index, tabItems, "MiniButton", GUI.ToolbarButtonSize.Fixed);

                        if (EditorGUI.EndChangeCheck())
                        {
                            UnityEditorUtility.RegisterUndo("GameTextSetterInspector-Undo", instance);

                            var selection = enumValues.ElementAtOrDefault(index);

                            Reflection.SetPrivateField(instance, "assetType", selection);

                            currentCategoryGuid = GetCategoryGuid(gameText, instance.TextGuid);
                        }
                    }

                    GUILayout.Space(4f);
                }
            }

            GUILayout.Space(2f);

            // 選択ウィンドウモード.

            if (assetType == GameTextSetter.AssetType.Internal)
            {
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
                            currentCategoryGuid = newCategoryGuid;

                            SetTextGuid(null);

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

                if (categoryChanged && string.IsNullOrEmpty(currentCategoryGuid))
                {
                    SetDevelopmentText(string.Empty);
                }
            }

            // 直接設定モード.

            if (assetType == GameTextSetter.AssetType.Extend)
            {
                EditorGUI.BeginChangeCheck();

                var newTextGuid = EditorGUILayout.DelayedTextField("Text Guid", instance.TextGuid);

                if (EditorGUI.EndChangeCheck())
                {
                    if (!string.IsNullOrEmpty(newTextGuid))
                    {
                        var text = gameText.FindText(newTextGuid);

                        if (!string.IsNullOrEmpty(text))
                        {
                            UnityEditorUtility.RegisterUndo("GameTextSetterInspector-Undo", instance);

                            SetTextGuid(newTextGuid);

                            UpdateSelectionAssetType(gameText, newTextGuid);
                        }
                        else
                        {
                            Debug.LogError("This guid is not defined asset.");
                        }
                    }
                    else
                    {
                        SetTextGuid(null);
                    }
                }
            }

            GUILayout.Space(2f);

            if (string.IsNullOrEmpty(currentTextGuid))
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

                        SetDevelopmentText(editText);
                    }
                }
            }
        }

        private void UpdateSelectionAssetType(GameText gameText, string textGuid)
        {
            var categoryGuid = GetCategoryGuid(gameText, textGuid);

            if (!string.IsNullOrEmpty(categoryGuid))
            {
                // 内包テキストに含まれるGuidだった場合は内包テキストモードに変更.
                Reflection.SetPrivateField(instance, "assetType", GameTextSetter.AssetType.Internal);
            }
        }

        private void SetTextGuid(string textGuid)
        {
            Reflection.InvokePrivateMethod(instance, "SetTextGuid", new object[] { textGuid });
        }

        private void SetDevelopmentText(string text)
        {
            Reflection.InvokePrivateMethod(instance, "SetDevelopmentText", new object[] { text });
        }

        private string GetCategoryGuid(GameText gameText, string textGuid)
        {
            var categoryEnum = (Enum)Reflection.InvokePrivateMethod(gameText, "FindCategoryEnumFromTextGuid", new object[] { textGuid });
            
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
