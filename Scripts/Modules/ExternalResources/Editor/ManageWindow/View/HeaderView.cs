
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.ExternalResource.Editor
{
    public sealed class HeaderView : LifetimeDisposable
    {
        //----- params -----

        private enum NameEditMode
        {
            None,
            New,
            Rename,
        }

        //----- field -----

        private AssetManagement assetManagement = null;

        private List<string> categoryNames = null;

        private string currentCategory = null;

        private string editCategoryName = string.Empty;

        private NameEditMode nameEditMode = NameEditMode.None;

        private string searchText = string.Empty;

        private Subject<Unit> onChangeSelectCategory = null;

        private Subject<Unit> onRequestRepaint = null;

        private bool initialized = false;

        //----- property -----

        public string Category { get { return currentCategory; } }

        public bool CategoryNameEdit { get { return nameEditMode != NameEditMode.None; } }

        //----- method -----

        public void Initialize(AssetManagement assetManagement)
        {
            if (initialized) { return; }

            this.assetManagement = assetManagement;

            categoryNames = assetManagement.GetAllCategoryNames().ToList();

            initialized = true;
        }

        public void DrawGUI()
        {
            var selection = categoryNames != null ? categoryNames.IndexOf(x => x == currentCategory) : -1;

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (nameEditMode != NameEditMode.None)
                {
                    GUILayout.FlexibleSpace();

                    EditorGUI.BeginChangeCheck();

                    editCategoryName = EditorGUILayout.DelayedTextField(editCategoryName, GUILayout.Width(250f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        if (!string.IsNullOrEmpty(editCategoryName))
                        {
                            switch (nameEditMode)
                            {
                                case NameEditMode.New:
                                    {
                                        categoryNames.Add(editCategoryName);

                                        currentCategory = editCategoryName;
                                    }
                                    break;

                                case NameEditMode.Rename:
                                    {
                                        if (currentCategory != editCategoryName)
                                        {
                                            assetManagement.RenameCategory(currentCategory, editCategoryName);

                                            currentCategory = editCategoryName;

                                            categoryNames = assetManagement.GetAllCategoryNames().ToList();
                                        }
                                    }
                                    break;
                            }
                        }

                        nameEditMode = NameEditMode.None;

                        editCategoryName = null;

                        if (onChangeSelectCategory != null)
                        {
                            onChangeSelectCategory.OnNext(Unit.Default);
                        }

                        if (onRequestRepaint != null)
                        {
                            onRequestRepaint.OnNext(Unit.Default);
                        }
                    }

                    if (GUILayout.Button("Cancel", EditorStyles.toolbarButton))
                    {
                        nameEditMode = NameEditMode.None;

                        editCategoryName = null;
                    }
                }
                else
                {
                    if (GUILayout.Button("追加", EditorStyles.toolbarButton))
                    {
                        nameEditMode = NameEditMode.New;
                    }

                    if (!string.IsNullOrEmpty(currentCategory))
                    {
                        if (GUILayout.Button("削除", EditorStyles.toolbarButton))
                        {
                            if (EditorUtility.DisplayDialog("確認", "カテゴリーを削除します", "実行", "中止"))
                            {
                                assetManagement.DeleteCategory(currentCategory);

                                categoryNames = assetManagement.GetAllCategoryNames().ToList();

                                if (onChangeSelectCategory != null)
                                {
                                    onChangeSelectCategory.OnNext(Unit.Default);
                                }

                                if (onRequestRepaint != null)
                                {
                                    onRequestRepaint.OnNext(Unit.Default);
                                }

                                return;
                            }
                        }

                        if (GUILayout.Button("リネーム", EditorStyles.toolbarButton))
                        {
                            nameEditMode = NameEditMode.Rename;

                            editCategoryName = currentCategory;

                            if (onRequestRepaint != null)
                            {
                                onRequestRepaint.OnNext(Unit.Default);
                            }

                            return;
                        }
                    }

                    GUILayout.FlexibleSpace();

                    // 検索.

                    Action<string> onChangeSearchText = x =>
                    {
                        searchText = x;
                    };

                    Action onSearchCancel = () =>
                    {
                        searchText = string.Empty;
                    };

                    EditorLayoutTools.DrawToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.Width(200f));
                    
                    // カテゴリー選択.

                    EditorGUI.BeginChangeCheck();

                    var categoryName = categoryNames.ElementAtOrDefault(selection);

                    var displayCategoryNames = GetDisplayCategoryNames();

                    displayCategoryNames = displayCategoryNames.OrderBy(x => x, new NaturalComparer()).ToList();

                    var index = displayCategoryNames.IndexOf(x => x == categoryName);

                    var displayLabels = displayCategoryNames.Select(x => ConvertSlashToUnicodeSlash(x)).ToArray();

                    index = EditorGUILayout.Popup(string.Empty, index, displayLabels, EditorStyles.toolbarDropDown, GUILayout.Width(180f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        selection = categoryNames.IndexOf(x => x == displayCategoryNames[index]);

                        currentCategory = categoryNames[selection];

                        if (onChangeSelectCategory != null)
                        {
                            onChangeSelectCategory.OnNext(Unit.Default);
                        }
                    }
                }
            }
        }
        
        private List<string> GetDisplayCategoryNames()
        {
            // 検索テキストでフィルタ.

            if (string.IsNullOrEmpty(searchText)) { return categoryNames; }
            
            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            return categoryNames.Where(x => x.IsMatch(keywords)).ToList();
        }

        private string ConvertSlashToUnicodeSlash(string text)
        {
            return text.Replace('/', '\u2215');
        }

        public IObservable<Unit> OnChangeSelectCategoryAsObservable()
        {
            return onChangeSelectCategory ?? (onChangeSelectCategory = new Subject<Unit>());
        }

        public IObservable<Unit> OnRequestRepaintAsObservable()
        {
            return onRequestRepaint ?? (onRequestRepaint = new Subject<Unit>());
        }
    }
}
