
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.TextData.Editor;

namespace Modules.TextData.Components
{
    public sealed class ToolbarView
    {
        //----- params -----

        //----- field -----
        
        private TextSetterInspector setterInspector = null;

        private ContentType contentType = default;

        private Subject<string> onCategoryChanged = null;

        private Subject<Unit> onUpdateSearchText = null;

        private Subject<Unit> onResetRecords = null;

        [NonSerialized]
        private bool initialized = false;
        
        //----- property -----

        public string SearchText { get; private set; }

        public string CategoryGuid  { get; set; }

        //----- method -----

        public void Initialize()
        {
            if (initialized) { return; }

            initialized = true;
        }

        public void DrawGUI()
        {
            setterInspector = TextSetterInspector.Current;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                GUILayout.Space(10f);

                if (setterInspector != null && setterInspector.Instance != null)
                {
                    contentType = setterInspector.Instance.ContentType;
                }
                else
                {
                    DrawSelectCategoryGUI();
                }

                DrawCategorySelectGUI();

                DrawSearchTextGUI();
            }
        }

        private void DrawSelectCategoryGUI()
        {
            var config = TextDataConfig.Instance;

            var distributionSetting = config.Distribution;

            if (distributionSetting.Enable)
            {
                var enumValues = Enum.GetValues(typeof(ContentType)).Cast<ContentType>().ToArray();

                var index = enumValues.IndexOf(x => x == contentType);

                var tabItems = enumValues.Select(x => x.ToString()).ToArray();
                            
                EditorGUI.BeginChangeCheck();

                index = GUILayout.Toolbar(index, tabItems, "MiniButton", GUI.ToolbarButtonSize.Fixed, GUILayout.MinWidth(200f));

                if (EditorGUI.EndChangeCheck())
                {
                    contentType = enumValues.ElementAtOrDefault(index);
                }
            }
            else
            {
                contentType = ContentType.Embedded;
            }
        }

        private void DrawCategorySelectGUI()
        {
            var textData = TextData.Instance;

            if (textData == null) { return; }

            var categories = textData.Categories.Where(x => x.ContentType == contentType).ToArray();
            
            var categoryIndex = string.IsNullOrEmpty(CategoryGuid) ? 0 : categories.IndexOf(x => x.Guid == CategoryGuid) + 1;

            var categoryLabels = categories.Select(x => x.DisplayName).ToArray();

            var labels = new List<string> { "None" };

            labels.AddRange(categoryLabels);

            EditorGUI.BeginChangeCheck();

            categoryIndex = EditorGUILayout.Popup(categoryIndex, labels.ToArray(), GUILayout.Width(250f));

            if (EditorGUI.EndChangeCheck())
            {
                if (setterInspector != null)
                {
                    UnityEditorUtility.RegisterUndo(setterInspector.Instance);
                }

                var newCategory = 1 <= categoryIndex ? categories[categoryIndex - 1] : null;

                var newCategoryGuid = newCategory != null ? newCategory.Guid : string.Empty;

                if (CategoryGuid != newCategoryGuid)
                {
                    if (setterInspector != null)
                    {
                        setterInspector.SetTextGuid(null);
                        setterInspector.SetDummyText(null);
                    }

                    CategoryGuid = newCategoryGuid;

                    if (onCategoryChanged != null)
                    {
                        onCategoryChanged.OnNext(CategoryGuid);
                    }
                }
            }

            GUILayout.FlexibleSpace();
        }

        private void DrawSearchTextGUI()
        {
            void OnChangeSearchText(string x)
            {
                SearchText = x;

                if (onUpdateSearchText != null)
                {
                    onUpdateSearchText.OnNext(Unit.Default);
                }
            }

            void OnSearchCancel()
            {
                SearchText = string.Empty;

                if (onUpdateSearchText != null)
                {
                    onUpdateSearchText.OnNext(Unit.Default);
                }
            };

            SearchText = EditorLayoutTools.DrawToolbarSearchTextField(SearchText, OnChangeSearchText, OnSearchCancel, GUILayout.Width(250));
        }

        public IObservable<string> OnCategoryChangedAsObservable()
        {
            return onCategoryChanged ?? (onCategoryChanged = new Subject<string>());
        }

        public IObservable<Unit> OnChangeSearchTextAsObservable()
        {
            return onUpdateSearchText ?? (onUpdateSearchText = new Subject<Unit>());
        }

        public IObservable<Unit> OnResetRecordsAsObservable()
        {
            return onResetRecords ?? (onResetRecords = new Subject<Unit>());
        }
    }
}