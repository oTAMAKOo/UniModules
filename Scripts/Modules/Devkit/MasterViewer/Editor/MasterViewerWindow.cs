
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Master;

namespace Modules.Devkit.MasterViewer
{
    public abstract class MasterViewerWindow<T, TMasterController> : SingletonEditorWindow<T>
        where T : MasterViewerWindow<T, TMasterController>
        where TMasterController : MasterController, new()
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(300f, 300f);

        //----- field -----

        private string searchText = null;

        private Vector2 scrollPosition = Vector2.zero;

        private TMasterController[] displayContents = null;

        private TMasterController[] masterControllers = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        protected virtual bool EnableLoadButton { get { return true; } }

        //----- method -----

        protected void Initialize()
        {
            if (initialized) { return; }

            var masterManager = MasterManager.Instance;

            titleContent = new GUIContent("MasterViewer");

            minSize = WindowSize;

            masterControllers = new TMasterController[0];

            // 読み込み先設定.
            var loadDirectory = GetLoadDirectory();

            masterManager.SetInstallDirectory(loadDirectory);

            // マスターロード実行.
            LoadAllMasterData();

            initialized = true;
        }

        void OnDestroy()
        {
            var windows = RecordViewerWindow.FindAllWindow();

            windows.ForEach(x => x.Close());
        }

        protected void SetMasterController(TMasterController[] masterControllers)
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

                if (EnableLoadButton)
                {
                    if (GUILayout.Button("Load", EditorStyles.toolbarButton, GUILayout.Width(40f)))
                    {
                        LoadAllMasterData();
                    }
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

        private void LoadAllMasterData()
        {
            var allMasters = GetAllMasters();
            
            var aesManaged = GetAesManaged();

            Func<IMaster, IObservable<Unit>> createMasterLoadObservable = master =>
            {
                return Observable.Defer(() => master.Load(aesManaged, false)).AsUnitObservable();
            };

            Action onLoadComplete = () =>
            {
                var masterControllers = new List<TMasterController>();

                foreach (var master in allMasters)
                {
                    var masterController = new TMasterController();

                    masterController.Initialize(master);

                    masterControllers.Add(masterController);
                }

                SetMasterController(masterControllers.ToArray());

                Repaint();
            };

            allMasters.Select(x => createMasterLoadObservable(x)).WhenAll()
                .Finally(() => EditorUtility.ClearProgressBar())
                .Subscribe(_ => onLoadComplete())
                .AddTo(Disposable);
        }

        private TMasterController[] GetDisplayMasters()
        {
            if (string.IsNullOrEmpty(searchText)) { return masterControllers; }

            var list = new List<TMasterController>();

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

        protected abstract string GetLoadDirectory();

        protected abstract AesManaged GetAesManaged();

        protected abstract IMaster[] GetAllMasters();
    }
}
