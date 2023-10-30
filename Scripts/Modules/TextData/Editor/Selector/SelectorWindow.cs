
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Modules.Devkit.AssemblyCompilation;

namespace Modules.TextData.Components
{
    public sealed class TextSelectData
    {
        public string TextGuid { get; private set; }
        public string Name { get; private set; }
        public string Text { get; private set; }

        public TextSelectData(string name, string textGuid, string text)
        {
            TextGuid = textGuid;
            Name = name;
            Text = text;
        }
    }

    public sealed class SelectorWindow : EditorWindow
    {
        //----- params -----

        private const string WindowTitle = "TextSelector";

        private static readonly Vector2 WindowSize = new Vector2(750f, 500f);
        
        //----- field -----

        private ToolbarView toolbarView = null;

        private RecordView recordView = null;

        private TextSelectData[] selectionCache = null;

        private LifetimeDisposable lifetimeDisposable = null;

        [NonSerialized]
        private bool initialized = false;

        private static SelectorWindow instance = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            if (instance != null)
            {
                instance.Close();
                instance = null;
            }

            instance = CreateInstance<SelectorWindow>();

            instance.Initialize();

            EditorApplication.delayCall += () =>
            {
                instance.Show();
            };
        }

        private void Initialize()
        {
            if (initialized) { return; }

            lifetimeDisposable = new LifetimeDisposable();

            minSize = WindowSize;

            titleContent = new GUIContent(WindowTitle);

            toolbarView = new ToolbarView();
            toolbarView.Initialize();

            recordView = new RecordView();
            recordView.Initialize();

            toolbarView.OnCategoryChangedAsObservable()
                .Subscribe(x =>
                    {
                        BuildSelectionInfos(x);
                    })
                .AddTo(lifetimeDisposable.Disposable);

            toolbarView.OnChangeSearchTextAsObservable()
                .Subscribe(x =>
                   {
                       var records = GetMatchOfList();

                       recordView.SetRecords(records);

                       Repaint();
                   })
                .AddTo(lifetimeDisposable.Disposable);

            toolbarView.OnResetRecordsAsObservable()
                .Subscribe(_ =>
                    {
                        recordView.RefreshRowHeights();
                    
                        Repaint();
                    })
                .AddTo(lifetimeDisposable.Disposable);

            Selection.selectionChanged += () => { Repaint(); };

            CompileNotification.OnCompileStartAsObservable()
                .Subscribe(_ => Close())
                .AddTo(lifetimeDisposable.Disposable);

            SetupSelectionCategory();
            BuildSelectionInfos(toolbarView.CategoryGuid);

            initialized = true;
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

            toolbarView.DrawGUI();

            recordView.DrawGUI();

            if(setterInspector == null)
            {
                EditorGUILayout.HelpBox("Not select TextDataSetter GameObject.", MessageType.Warning);
            }
        }

        private void SetupSelectionCategory()
        {
            var textData = TextData.Instance;

            var setterInspector = TextSetterInspector.Current;

            if (setterInspector != null && setterInspector.Instance != null)
            {
                toolbarView.CategoryGuid = TextSetterInspector.GetCategoryGuid(textData, setterInspector.Instance.TextGuid);
            }
        }

        private void BuildSelectionInfos(string categoryGuid)
        {
            var textData = TextData.Instance;

            var categoryTexts = GetCategoryTextGuids(textData, categoryGuid);

            var list = new List<TextSelectData>();

            foreach (var categoryText in categoryTexts)
            {
                var text = textData.FindText(categoryText.Value);

                var info = new TextSelectData(categoryText.Key, categoryText.Value, text);

                list.Add(info);
            }
            
            selectionCache = list.ToArray();

            recordView.SetRecords(selectionCache);
        }

        private TextSelectData[] GetMatchOfList()
        {
            var searchText = toolbarView.SearchText;

            if (string.IsNullOrEmpty(searchText)) { return selectionCache; }

            var list = new List<TextSelectData>();

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
    }
}
