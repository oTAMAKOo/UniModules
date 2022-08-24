
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Master;

namespace Modules.Devkit.MasterViewer
{
    public abstract class MasterViewerWindow<T> : SingletonEditorWindow<T> where T : MasterViewerWindow<T>
    {
        //----- params -----

        private static readonly Vector2 WindowSize = new Vector2(350f, 500f);

        private static readonly Color EditedColor = new Color(0.2f, 1f, 1f, 1f);

        //----- field -----

        private string searchText = null;

        private Vector2 scrollPosition = Vector2.zero;

        private MasterController[] displayContents = null;

        private List<MasterController> masterControllers = null;

        private bool loadMasterRequest = false;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        protected virtual bool EnableLoadButton { get { return true; } }

        //----- method -----

        protected void Initialize()
        {
            if (initialized) { return; }
            
            titleContent = new GUIContent("MasterViewer");

            minSize = WindowSize;

            masterControllers = new List<MasterController>();

            loadMasterRequest = true;

            OnInitialize();

            initialized = true;
        }

        void OnDestroy()
        {
            CloseAllRecordWindow();
        }

        void OnGUI()
        {
            Initialize();

            // マスターロード実行.

            if (loadMasterRequest)
            {
                LoadAllMasterData().Forget();
            }

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
                        LoadAllMasterData().Forget();
                    }
                }

                if (GUILayout.Button("Close All", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    CloseAllRecordWindow();
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

                            var color = content.HasChangedRecord ? EditedColor : Color.white;

                            using (new BackgroundColorScope(color))
                            {
                                if (GUILayout.Button(masterName))
                                {
                                    var recordViewerWindow = RecordWindow.Open(content);

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

        public async UniTask LoadAllMasterData()
        {
            loadMasterRequest = false;

            var isComplete = false;

            var allMasters = GetAllMasters();
            
            var cryptoKey = GetCryptoKey();

            var loadFinishCount = 0;
            var totalMasterCount = allMasters.Length;

            Action onLoadFinish = () =>
            {
                loadFinishCount++;

                if (isComplete) { return; }

                EditorUtility.DisplayProgressBar("progress", "Loading all masters", (float)loadFinishCount / totalMasterCount);
            };

            Action onLoadComplete = () =>
            {
                masterControllers = new List<MasterController>();

                foreach (var master in allMasters)
                {
                    var masterController = new MasterController();

                    masterController.Initialize(master);

                    masterControllers.Add(masterController);
                }

                displayContents = GetDisplayMasters();

                Repaint();

                isComplete = true;
            };

            var tasks = new List<UniTask>();

            foreach (var master in allMasters)
            {
                var observable = UniTask.Defer(async () =>
				{
					var loadResult = await master.Load(cryptoKey, false);

					if (loadResult != null && loadResult.Item1)
					{
						onLoadFinish();
					}
				});

				tasks.Add(observable);
            }

			await UniTask.WhenAll(tasks);

			onLoadComplete();

			EditorUtility.ClearProgressBar();
		}

        private MasterController[] GetDisplayMasters()
        {
            if (string.IsNullOrEmpty(searchText)) { return masterControllers.ToArray(); }

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

        private void CloseAllRecordWindow()
        {
            var recordWindows = Resources.FindObjectsOfTypeAll<RecordWindow>();

            foreach (var recordWindow in recordWindows)
            {
                if (recordWindow == null) { continue; }

                try
                {
                    recordWindow.Close();

                    UnityUtility.SafeDelete(recordWindow);
                }
                catch
                {
                    // ignored
                }
            }
        }

        protected virtual void OnInitialize() { }

        protected abstract AesCryptoKey GetCryptoKey();

        protected abstract IMaster[] GetAllMasters();
    }
}
