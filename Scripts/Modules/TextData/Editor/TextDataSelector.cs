
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.AssemblyCompilation;
using NUnit.Framework.Interfaces;

namespace Modules.TextData.Components
{
    public sealed class TextDataSelector : ScriptableWizard
    {
        //----- params -----

        private const string WindowTitle = "TextDataSelector";

        private sealed class SelectionInfo
        {
            public string TextGuid { get; private set; }
            public string Name { get; private set; }
            public string Text { get; private set; }

            public SelectionInfo(string name, string textGuid, string text)
            {
                TextGuid = textGuid;
                Name = name;
                Text = text;
            }
        }

        //----- field -----

        private string categoryGuid = null;

        private TextDataSelectorScrollView scrollView = null;

        private string searchText = null;

        private SelectionInfo[] selectionCache = null;

        private LifetimeDisposable lifetimeDisposable = null;

        private static TextDataSelector instance = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            if (instance != null)
            {
                instance.Close();
                instance = null;
            }

            instance = DisplayWizard<TextDataSelector>(WindowTitle);

            instance.Initialize();
            instance.SetupSelectionCategory();
            instance.BuildSelectionInfos();
        }

        private void Initialize()
        {
            lifetimeDisposable = new LifetimeDisposable();

            scrollView = new TextDataSelectorScrollView();

            Selection.selectionChanged += () => { Repaint(); };

            CompileNotification.OnCompileStartAsObservable()
                .Subscribe(_ => Close())
                .AddTo(lifetimeDisposable.Disposable);

            scrollView.OnSelectAsObservable()
                .Subscribe(_ => Close())
                .AddTo(lifetimeDisposable.Disposable);

            scrollView.Contents = GetMatchOfList();
        }

        void OnDestroy()
        {
            if(lifetimeDisposable != null)
            {
                lifetimeDisposable.Dispose();
                lifetimeDisposable = null;
            }
        }

        void OnGUI()
        {
            var setterInspector = TextSetterInspector.Current;

            if(setterInspector == null)
            {
                EditorGUILayout.HelpBox("Need Select TextDataSetter GameObject.", MessageType.Info);
                return;
            }

            var setter = TextSetterInspector.Current.Instance;

            if (setter == null) { return; }

            var textData = TextData.Instance;

            if (textData != null)
            {
                // Toolbar.

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
                {
                    var categories = textData.Categories.Where(x => x.ContentType == setter.ContentType).ToArray();

                    // Noneが入るので1ずれる.
                    var categoryIndex = categories.IndexOf(x => x.Guid == categoryGuid) + 1;

                    var categoryLabels = categories.Select(x => x.DisplayName).ToArray();

                    var labels = new List<string> { "None" };

                    labels.AddRange(categoryLabels);

                    EditorGUI.BeginChangeCheck();

                    categoryIndex = EditorGUILayout.Popup(categoryIndex, labels.ToArray());

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo(instance);
                    
                        var newCategory = 1 <= categoryIndex ? categories[categoryIndex - 1] : null;

                        var newCategoryGuid = newCategory != null ? newCategory.Guid : string.Empty;

                        if (categoryGuid != newCategoryGuid)
                        {
                            setterInspector.SetTextGuid(null);
                            setterInspector.SetDummyText(null);

                            categoryGuid = newCategoryGuid;

                            BuildSelectionInfos();
                        }
                    }

                    GUILayout.FlexibleSpace();

                    // クリア.

                    if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
                    {
                        setterInspector.SetTextGuid(null);
                    }

                    GUILayout.Space(4f);

                    // 検索.

                    void OnChangeSearchText(string x)
                    {
                        searchText = x;
                        scrollView.Contents = GetMatchOfList();
                        EditorApplication.delayCall += () => { Repaint(); };
                    }

                    void OnSearchCancel()
                    {
                        searchText = string.Empty;
                        scrollView.Contents = GetMatchOfList();
                        EditorApplication.delayCall += () => { Repaint(); };
                    }

                    EditorLayoutTools.DrawToolbarSearchTextField(searchText, OnChangeSearchText, OnSearchCancel, GUILayout.Width(250f));
                }

                EditorGUILayout.Separator();

                // Contents.

                scrollView.Draw();
            }
            else
            {
                EditorGUILayout.HelpBox("TextData not found", MessageType.Warning);
            }
        }

        private void SetupSelectionCategory()
        {
            var textData = TextData.Instance;

            var setter = TextSetterInspector.Current.Instance;

            categoryGuid = TextSetterInspector.GetCategoryGuid(textData, setter.TextGuid);
        }

        private void BuildSelectionInfos()
        {
            var textData = TextData.Instance;

            var setter = TextSetterInspector.Current.Instance;
            var setterInspector = TextSetterInspector.Current;

            var categoryTexts = GetCategoryTextGuids(textData, categoryGuid);

            var list = new List<SelectionInfo>();

            foreach (var categoryText in categoryTexts)
            {
                var text = textData.FindText(categoryText.Value);

                var info = new SelectionInfo(categoryText.Key, categoryText.Value, text);

                list.Add(info);
            }
            
            selectionCache = list.ToArray();

            scrollView.Setter = setter;
            scrollView.SetterInspector = setterInspector;
            scrollView.Contents = GetMatchOfList();
        }

        private SelectionInfo[] GetMatchOfList()
        {
            if (string.IsNullOrEmpty(searchText)) { return selectionCache; }

            var list = new List<SelectionInfo>();

            string[] keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < keywords.Length; ++i) keywords[i] = keywords[i].ToLower();

            foreach (var item in selectionCache)
            {
                var isMatch = item.Name.IsMatch(keywords) || item.Text.IsMatch(keywords);

                if (isMatch)
                {
                    list.Add(item);
                }
            }
            
            return list.ToArray();
        }

        private IReadOnlyDictionary<string, string> GetCategoryTextGuids(TextData textData, string categoryGuid)
        {
            var categoryTexts = new Dictionary<string, string>();

            var textInfos = textData.Texts.Values.Where(x => x.categoryGuid == categoryGuid).ToArray();

            foreach (var textInfo in textInfos)
            {
                var enumName = textData.GetEnumName(textInfo.textGuid);
                
                categoryTexts.Add(enumName, textInfo.textGuid);
            }

            return categoryTexts;
        }

        private sealed class TextDataSelectorScrollView : EditorGUIFastScrollView<SelectionInfo>
        {
            //----- params -----

            //----- field -----

            public string[] textGuids { get; set; }

            private Subject<Unit> onSelect = null;

            //----- property -----

            public override Direction Type { get { return Direction.Vertical; } }

            public TextSetter Setter { get; set; }

            public TextSetterInspector SetterInspector { get; set; }

            //----- method -----

            protected override void DrawContent(int index, SelectionInfo content)
            {
                var highlight = Setter.TextGuid == content.TextGuid;

                var originBackgroundColor = GUI.backgroundColor;

                using (new BackgroundColorScope(highlight ? new Color(0.6f, 0.8f, 0.85f) : new Color(0.95f, 0.95f, 0.95f)))
                {
                    var size = EditorStyles.label.CalcSize(new GUIContent(content.Text));

                    size.y += 6f;

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea, GUILayout.Height(size.y)))
                    {
                        var labelStyle = new GUIStyle("IN TextField")
                        {
                            alignment = TextAnchor.MiddleLeft,
                        };

                        GUILayout.Space(10f);

                        GUILayout.Label(content.Name, labelStyle, GUILayout.MinWidth(220f), GUILayout.Height(size.y));

                        GUILayout.Label(content.Text, labelStyle, GUILayout.MaxWidth(500f), GUILayout.Height(size.y));

                        GUILayout.FlexibleSpace();

                        using (new EditorGUILayout.VerticalScope())
                        {
                            var buttonHeight = 16f;

                            GUILayout.Space((size.y - buttonHeight) * 0.5f);

                            using (new BackgroundColorScope(originBackgroundColor))
                            {
                                if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(60f)))
                                {
                                    UnityEditorUtility.RegisterUndo(Setter);
                                    
                                    if (!string.IsNullOrEmpty(content.TextGuid))
                                    {
                                        SetterInspector.SetTextGuid(content.TextGuid);

                                        SetterInspector.Repaint();

                                        if (onSelect != null)
                                        {
                                            onSelect.OnNext(Unit.Default);
                                        }
                                    }
                                }
                            }

                            GUILayout.Space((size.y - buttonHeight) * 0.5f);
                        }

                        GUILayout.Space(8f);
                    }
                }
            }

            public IObservable<Unit> OnSelectAsObservable()
            {
                return onSelect ?? (onSelect = new Subject<Unit>());
            }
        }
    }
}
