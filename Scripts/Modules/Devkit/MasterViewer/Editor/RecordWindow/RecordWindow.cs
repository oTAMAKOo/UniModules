
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Devkit.MasterViewer
{
    public sealed class RecordWindow : EditorWindow
    {
        //----- params -----

        private static readonly Vector2 WindowSize = new Vector2(300f, 300f);

        //----- field -----

        private ToolbarView toolbarView = null;

        private RecordView recordView = null;

        private MasterController masterController = null;

        private LifetimeDisposable lifetimeDisposable = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static RecordWindow Open(MasterController masterController)
        {
            RecordWindow recordWindow = null;

            // 開いているウィンドウ取得.
            var window = FindWindow(masterController.MasterType);

            // 1つのマスターにつき1ウィンドウまでしか開かない.
            if (window == null)
            {
                window = CreateInstance<RecordWindow>();

                window.Initialize(masterController);

                EditorApplication.delayCall += () =>
                {
                    window.Show();
                };

                recordWindow = window;
            }

            window.Focus();

            return recordWindow;
        }

        private static RecordWindow FindWindow(Type masterType)
        {
            return Resources.FindObjectsOfTypeAll<RecordWindow>()
                .Where(x => x.masterController != null)
                .FirstOrDefault(x => x.masterController.MasterType == masterType);
        }

        private void Initialize(MasterController masterController)
        {
            if (initialized) { return; }

            this.masterController = masterController;

            minSize = WindowSize;
            titleContent = new GUIContent(masterController.GetDisplayMasterName());

            lifetimeDisposable = new LifetimeDisposable();

            toolbarView = new ToolbarView();
            toolbarView.Initialize(masterController);

            recordView = new RecordView();
            recordView.Initialize(masterController);

            toolbarView.OnChangeSearchTextAsObservable()
                .Subscribe(x =>
                   {
                       var records = GetDisplayRecords();

                       recordView.SetRecords(records);
                   })
                .AddTo(lifetimeDisposable.Disposable);

            toolbarView.OnResetRecordsAsObservable()
                .Subscribe(_ => recordView.RefreshRowHeights())
                .AddTo(lifetimeDisposable.Disposable);

            initialized = true;
        }

        void OnGUI()
        {
            if (masterController == null)
            {
                Close();
                return;
            }

            toolbarView.DrawGUI();

            recordView.DrawGUI();
        }

        private object[] GetDisplayRecords()
        {
            var records = masterController.Records;
            
            var searchedRecords = GetSearchedRecords(toolbarView.SearchText);

            if (searchedRecords != null)
            {
                records = searchedRecords;
            }

            return records;
        }

        private object[] GetSearchedRecords(string searchText)
        {
            const char SearchValueSeparator = ':';

            if (string.IsNullOrEmpty(searchText)) { return null; }

            var list = new List<object>();

            var searchTextKeywords = searchText;

            var searchValueName = string.Empty;
            
            if (searchText.Contains(SearchValueSeparator))
            {
                var texts = searchText.Split(new char[] { SearchValueSeparator }, StringSplitOptions.RemoveEmptyEntries);

                searchValueName = texts.Select(x => x.Trim()).FirstOrDefault();
            }

            if (!string.IsNullOrEmpty(searchValueName))
            {
                searchValueName = searchValueName.ToLower();

                var searchValueNameIndex = searchText.IndexOf(SearchValueSeparator);

                searchTextKeywords = searchText.SafeSubstring(searchValueNameIndex + 1);
            }

            var keywords = searchTextKeywords.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].Trim().ToLower();
            }

            var records = masterController.Records;

            var valueNames = masterController.GetValueNames();

            foreach (var record in records)
            {
                foreach (var valueName in valueNames)
                {
                    if (!string.IsNullOrEmpty(searchValueName))
                    {
                        if (searchValueName != valueName.ToLower()) { continue; }
                    }

                    var value = masterController.GetValue(record, valueName);

                    if (value != null)
                    {
                        var valueStr = value.ToString().ToLower();

                        // or 検索なのでIsMatchを使わない.
                        if (keywords.Any(x => valueStr.Contains(x)))
                        {
                            list.Add(record);
                            break;
                        }
                    }
                }
            }

            return list.ToArray();
        }

        public IObservable<Unit> OnChangeRecordAsObservable()
        {
            return recordView.OnChangeRecordAsObservable();
        }
    }
}