
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.CompileNotice;

namespace Modules.GameText.Components
{
    public class GameTextSelector : ScriptableWizard
    {
        //----- params -----

        private class SelectionInfo
        {
            public string TextGuid { get; private set; }
            public string Name { get; private set; }
            public string Text { get; private set; }

            public SelectionInfo(string textGuid, string name, string text)
            {
                TextGuid = textGuid;
                Name = name;
                Text = text;
            }
        }

        //----- field -----

        private string categoryGuid = null;

        private GameTextSelectorScrollView scrollView = null;

        private string searchText = null;

        private SelectionInfo[] selectionCache = null;

        private LifetimeDisposable lifetimeDisposable = null;

        private static GameTextSelector instance = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            if (instance != null)
            {
                instance.Close();
                instance = null;
            }

            var gameText = GameText.Instance;
            
            var setter = GameTextSetterInspector.Current.Instance;

            var category = gameText.FindCategoryDefinitionEnum(setter.CategoryGuid);

            var titleText = string.Format("GameTextSelector : {0}", category.ToLabelName());

            instance = DisplayWizard<GameTextSelector>(titleText);

            instance.Initialize();
        }

        private void Initialize()
        {
            lifetimeDisposable = new LifetimeDisposable();

            scrollView = new GameTextSelectorScrollView();

            Selection.selectionChanged += () => { Repaint(); };

            CompileNotification.OnCompileStartAsObservable()
                .Subscribe(_ => Close())
                .AddTo(lifetimeDisposable.Disposable);

            scrollView.OnSelectAsObservable()
                .Subscribe(_ => Close())
                .AddTo(lifetimeDisposable.Disposable);

            scrollView.Contents = GetMatchOfList();

            BuildSelectionInfos();
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
            var setterInspector = GameTextSetterInspector.Current;

            if(setterInspector == null)
            {
                EditorGUILayout.HelpBox("Need Select GameTextSetter GameObject.", MessageType.Info);
                return;
            }

            var gameText = GameText.Instance;

            var setter = GameTextSetterInspector.Current.Instance;

            if (setter == null) { return; }

            if(categoryGuid != setter.CategoryGuid)
            {
                BuildSelectionInfos();
            }

            if (selectionCache.Any())
            {
                EditorGUILayout.Separator();

                // Toolbar.

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
                {
                    // クリア.

                    if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
                    {
                        setter.SetText(null);
                    }

                    GUILayout.FlexibleSpace();

                    // 検索.

                    Action<string> onChangeSearchText = x =>
                    {
                        searchText = x;

                        scrollView.Contents = GetMatchOfList();

                        EditorApplication.delayCall += () =>
                        {
                            Repaint();
                        };
                    };

                    Action onSearchCancel = () =>
                    {
                        searchText = string.Empty;

                        scrollView.Contents = GetMatchOfList();

                        EditorApplication.delayCall += () =>
                        {
                            Repaint();
                        };
                    };

                    EditorLayoutTools.DrawToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.Width(250f));
                }

                EditorGUILayout.Separator();

                // Contents.
                
                scrollView.Setter = setter;
                scrollView.SetterInspector = setterInspector;
                scrollView.CategoryTexts = gameText.FindCategoryTexts(setter.CategoryGuid);

                scrollView.Draw();
            }
            else
            {
                EditorGUILayout.HelpBox("GameText not found", MessageType.Warning);
            }
        }

        private void BuildSelectionInfos()
        {
            var gameText = GameText.Instance;

            categoryGuid = GameTextSetterInspector.Current.Instance.CategoryGuid;

            var categoryTexts = gameText.FindCategoryTexts(categoryGuid);

            var list = new List<SelectionInfo>();

            foreach (var textData in categoryTexts)
            {
                var text = gameText.FindText(textData.Value);

                var info = new SelectionInfo(textData.Value, textData.Key.ToString(), text);

                list.Add(info);
            }
            
            selectionCache = list.ToArray();

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

        private sealed class GameTextSelectorScrollView : EditorGUIFastScrollView<SelectionInfo>
        {
            //----- params -----

            //----- field -----

            private Subject<Unit> onSelect = null;

            //----- property -----

            public override Direction Type { get { return Direction.Vertical; } }

            public GameTextSetter Setter { get; set; }

            public GameTextSetterInspector SetterInspector { get; set; }

            public IReadOnlyDictionary<Enum, string> CategoryTexts { get; set; }

            //----- method -----

            protected override void DrawContent(int index, SelectionInfo content)
            {
                var highlight = Setter.TextGuid == content.TextGuid;

                var originBackgroundColor = GUI.backgroundColor;

                using (new BackgroundColorScope(highlight ? new Color(0.6f, 1f, 0.9f) : new Color(0.95f, 0.95f, 0.95f)))
                {
                    var size = EditorStyles.label.CalcSize(new GUIContent(content.Text));

                    size.y += 6f;

                    using (new EditorGUILayout.HorizontalScope(EditorLayoutTools.TextAreaStyle, GUILayout.Height(size.y)))
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
                            var buttonHeight = 18f;

                            GUILayout.Space((size.y - buttonHeight) * 0.5f);

                            using (new BackgroundColorScope(originBackgroundColor))
                            {
                                if (GUILayout.Button("Select", GUILayout.Width(75f), GUILayout.Height(buttonHeight)))
                                {
                                    UnityEditorUtility.RegisterUndo("GameTextSelector-Select", Setter);

                                    var textInfo = CategoryTexts.FirstOrDefault(x => x.Value == content.TextGuid);

                                    if (!textInfo.Equals(default(KeyValuePair<Enum, string>)))
                                    {
                                        Setter.SetText(textInfo.Key);
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
