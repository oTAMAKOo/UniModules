
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using UniRx;

namespace Modules.Devkit.MasterViewer
{
    public abstract class MasterViewerWindow<T> : SingletonEditorWindow<T> where T : MasterViewerWindow<T>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(300f, 300f);

        //----- field -----

        private string searchText = null;

        private Vector2 scrollPosition = Vector2.zero;

        private MasterController[] displayContents = null;

        private MasterController[] masterControllers = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----


        //----- method -----

        protected void Initialize()
        {
            if (initialized) { return; }

            titleContent = new GUIContent("MasterViewer");

            minSize = WindowSize;

            masterControllers = new MasterController[0];

            // マスターロード実行.
            LoadMasterData().Subscribe().AddTo(Disposable);

            initialized = true;
        }

        protected void SetMasterController(MasterController[] masterControllers)
        {
            this.masterControllers = masterControllers;

            displayContents = GetDisplayMasters();
        }

        void OnGUI()
        {
            Initialize();

            // Toolbar.

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                Action<string> onChangeSearchText = x =>
                {
                    searchText = x;
                    displayContents = GetDisplayMasters();
                };

                Action onSearchCancel = () =>
                {
                    searchText = string.Empty;
                    displayContents = GetDisplayMasters();
                };

                EditorLayoutTools.DrawToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.MinWidth(150f));

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(40f)))
                {
                    LoadMasterData().Subscribe(_ => Repaint()).AddTo(Disposable);
                }

                if (GUILayout.Button("Close All", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    var windows = RecordViewerWindow.FindAllWindow();

                    windows.ForEach(x => x.Close());
                }
            }

            // ScrollView.

            if (displayContents != null)
            {
                GUILayout.Space(2f);

                using (new ContentsScope())
                {
                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                    {
                        for (var i = 0; i < displayContents.Length ; i++)
                        {
                            var content = displayContents[i];

                            var masterName = content.GetDisplayMasterName();

                            var color = content.HasChangedRecord ? Color.yellow : Color.white;

                            using (new BackgroundColorScope(color))
                            {
                                if (GUILayout.Button(masterName))
                                {
                                    var recordViewerWindow = RecordViewerWindow.Open(content);

                                    if (recordViewerWindow != null)
                                    {
                                        recordViewerWindow.OnChangeRecordAsObservable()
                                            .Subscribe(_ => Repaint())
                                            .AddTo(Disposable);
                                    }
                                }
                            }

                            if (i < displayContents.Length - 1)
                            {
                                GUILayout.Space(3f);
                            }
                        }

                        scrollPosition = scrollViewScope.scrollPosition;
                    }
                }

                GUILayout.Space(5f);
            }
        }

        private MasterController[] GetDisplayMasters()
        {
            if (string.IsNullOrEmpty(searchText)) { return masterControllers; }

            var list = new List<MasterController>();

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            foreach (var item in masterControllers)
            {
                if (item.MasterType.Name.IsMatch(keywords))
                {
                    list.Add(item);
                }
            }

            return list.ToArray();
        }

        protected abstract IObservable<Unit> LoadMasterData();
    }
}
