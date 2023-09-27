
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.TextData.Components
{
    public sealed class ToolbarView
    {
        //----- params -----

        //----- field -----
        
        private TextSetterInspector setterInspector = null;

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

            if(setterInspector == null)
            {
                EditorGUILayout.HelpBox("Need Select TextDataSetter GameObject.", MessageType.Info);
                return;
            }

            var setter = TextSetterInspector.Current.Instance;

            if (setter == null) { return; }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                GUILayout.Space(10f);

                DrawCategorySelectGUI();

                DrawSearchTextGUI();
            }
        }

        private void DrawCategorySelectGUI()
        {
            var textData = TextData.Instance;

            if (textData == null) { return; }

            var setter = TextSetterInspector.Current.Instance;

            if (setter == null) { return; }

            var categories = textData.Categories.Where(x => x.ContentType == setter.ContentType).ToArray();

            // Noneが入るので1ずれる.
            var categoryIndex = categories.IndexOf(x => x.Guid == CategoryGuid) + 1;

            var categoryLabels = categories.Select(x => x.DisplayName).ToArray();

            var labels = new List<string> { "None" };

            labels.AddRange(categoryLabels);

            EditorGUI.BeginChangeCheck();

            categoryIndex = EditorGUILayout.Popup(categoryIndex, labels.ToArray(), GUILayout.Width(250f));

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo(setter);
                    
                var newCategory = 1 <= categoryIndex ? categories[categoryIndex - 1] : null;

                var newCategoryGuid = newCategory != null ? newCategory.Guid : string.Empty;

                if (CategoryGuid != newCategoryGuid)
                {
                    setterInspector.SetTextGuid(null);
                    setterInspector.SetDummyText(null);

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