
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using R3;
using Extensions;

namespace Modules.TextData.Components
{
    public sealed class TextSelectData
    {
        public string TextIdentifier { get; private set; }

        public string TextGuid { get; private set; }
        
        public string Name { get; private set; }
        
        public string Text { get; private set; }

        public TextSelectData(string textGuid, string textIdentifier, string name, string text)
        {
            TextIdentifier = textIdentifier;
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

        [SerializeField]
        private string savedCategoryGuid = null;

        [SerializeField]
        private TextType savedTextType = default;

        [SerializeField]
        private string savedSearchText = null;

        [SerializeField]
        private Vector2 savedScrollPosition = Vector2.zero;

        [SerializeField]
        private bool stateRestored = false;

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
            // 既に開かれている場合はフォーカスを当てて終了.
            var existingWindows = Resources.FindObjectsOfTypeAll<SelectorWindow>();

            if (0 < existingWindows.Length)
            {
                instance = existingWindows[0];
                instance.Focus();
                return;
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

            // 初回は Setter から初期カテゴリを取得、2回目以降は保存済みの状態を復元.
            if (stateRestored)
            {
                toolbarView.CategoryGuid = savedCategoryGuid;
                toolbarView.Type = savedTextType;
                toolbarView.SearchText = savedSearchText;
                recordView.ScrollPosition = savedScrollPosition;

                // 保存済みカテゴリが空の場合は Setter から補完.
                if (string.IsNullOrEmpty(toolbarView.CategoryGuid))
                {
                    SetupSelectionCategory();

                    savedCategoryGuid = toolbarView.CategoryGuid;
                }
            }
            else
            {
                SetupSelectionCategory();

                savedCategoryGuid = toolbarView.CategoryGuid;
                savedTextType = toolbarView.Type;
                stateRestored = true;
            }

            toolbarView.OnContentTypeChangedAsObservable()
                .Subscribe(_ =>
                    {
                        savedTextType = toolbarView.Type;

                        BuildSelectionInfos(toolbarView.CategoryGuid);
                    })
                .AddTo(lifetimeDisposable.Disposable);

            toolbarView.OnCategoryChangedAsObservable()
                .Subscribe(x =>
                    {
                        savedCategoryGuid = x;

                        BuildSelectionInfos(x);
                    })
                .AddTo(lifetimeDisposable.Disposable);

            toolbarView.OnChangeSearchTextAsObservable()
                .Subscribe(_ =>
                   {
                       savedSearchText = toolbarView.SearchText;

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
            Initialize();

            var setterInspector = TextSetterInspector.Current;

            // ドメインリロード直後などで一覧が失われている場合は再構築.
            if (selectionCache == null)
            {
                BuildSelectionInfos(toolbarView.CategoryGuid);
            }

            toolbarView.DrawGUI();

            recordView.DrawGUI();

            // スクロール位置をシリアライズ対象に反映（コンパイル跨ぎで復元するため）.
            savedScrollPosition = recordView.ScrollPosition;

            if(setterInspector == null)
            {
                EditorGUILayout.HelpBox("Not select TextDataSetter GameObject.", MessageType.Warning);
            }
        }

        private void SetupSelectionCategory()
        {
            var textData = TextData.Instance;

            if (textData == null) { return; }

            var setterInspector = TextSetterInspector.Current;

            if (setterInspector != null && setterInspector.Instance != null)
            {
                toolbarView.CategoryGuid = TextSetterInspector.GetCategoryGuid(textData, setterInspector.Instance.TextGuid);
            }
        }

        private void BuildSelectionInfos(string categoryGuid)
        {
            var textData = TextData.Instance;

            if (textData == null) { return; }

            var categoryTexts = GetCategoryTextGuids(textData, categoryGuid);

            var list = new List<TextSelectData>();

            foreach (var categoryText in categoryTexts)
            {
                var enumName = categoryText.Key;

                var textGuid = categoryText.Value;
                
                var textInfo = textData.FindTextInfo(textGuid);

                var info = new TextSelectData(textGuid, textInfo.textIdentifier, enumName, textInfo.text);

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

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

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

            if (string.IsNullOrEmpty(categoryGuid)) { return categoryTexts; }

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
