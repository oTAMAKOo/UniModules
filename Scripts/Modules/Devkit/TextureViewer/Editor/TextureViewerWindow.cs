
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.TextureViewer
{
    public enum DisplayMode
    {
        Texture,
        Compress,
    }

    public sealed class TextureViewerWindow : SingletonEditorWindow<TextureViewerWindow>
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(700f, 500f);

        //----- field -----

        private ToolbarView toolbarView = null;
        private InfoTreeView infoTreeView = null;
        private FooterView footerView = null;

        private TextureInfo[] textureInfos = null;

        private bool initalLoading = false;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        public BuildTargetGroup Platform { get; private set; }

        public DisplayMode DisplayMode { get; private set; }

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();
        }

        private void Initialize()
        {
            if (initialized) { return; }
            
            titleContent = new GUIContent("TextureViewer");
            minSize = WindowSize;

            DisplayMode = DisplayMode.Texture;

            // 非同期で初期化.
            Observable.FromMicroCoroutine(() => InitializeAsync())
                .Subscribe()
                .AddTo(Disposable);

            initialized = true;
        }

        private IEnumerator InitializeAsync()
        {
            // テクスチャ情報読み込み.
            var loadTextureInfoYield = Observable.FromMicroCoroutine<TextureInfo[]>(observer => LoadTextureInfos(observer)).ToYieldInstruction(false);

            while (!loadTextureInfoYield.IsDone)
            {
                yield return null;
            }

            if (loadTextureInfoYield.HasResult)
            {
                textureInfos = loadTextureInfoYield.Result;
            }

            // バックグラウンド読み込み.

            initalLoading = true;

            Observable.FromMicroCoroutine(() => LoadMainTextureBackground())
                .Subscribe()
                .AddTo(Disposable);

            // View初期化.

            InitializeViews();
            
            // 1秒後に表示.

            Observable.Timer(TimeSpan.FromSeconds(1))
                .Subscribe(_ =>
                   {
                       initalLoading = false;

                       EditorUtility.ClearProgressBar();

                       Show();
                   })
                .AddTo(Disposable);
        }

        private void InitializeViews()
        {
            var platform = EditorUserBuildSettings.selectedBuildTargetGroup;

            toolbarView = new ToolbarView();
            toolbarView.Initialize(platform);

            infoTreeView = new InfoTreeView();
            infoTreeView.Initialize(textureInfos, platform, DisplayMode);

            footerView = new FooterView();
            footerView.Initialize();

            // 表示モード変更.
            toolbarView.OnChangeDisplayModeAsObservable()
                .Subscribe(x =>
                   {
                       DisplayMode = x;
                       infoTreeView.SetDisplayMode(x);
                   })
                .AddTo(Disposable);

            // プラットフォーム変更.
            toolbarView.OnChangePlatformAsObservable()
                .Subscribe(x =>
                   {
                       platform = x;
                       infoTreeView.SetPlatform(x);
                   })
                .AddTo(Disposable);

            // フィルタテキスト変更.
            toolbarView.OnUpdateSearchTextAsObservable()
                .Subscribe(x =>
                   {
                       var infos = GetTextureInfos(x);
                       infoTreeView.SetContents(infos, true);
                   })
                .AddTo(Disposable);

            // ソートリセット.
            toolbarView.OnRequestSortResetAsObservable()
                .Subscribe(_ => infoTreeView.ResetSort())
                .AddTo(Disposable);

            // 選択変更.
            infoTreeView.OnSelectionChangedAsObservable()
                .Subscribe(x => footerView.SetSelection(x))
                .AddTo(Disposable);
        }

        void OnGUI()
        {
            Initialize();
            
            toolbarView.DrawGUI();

            infoTreeView.DrawGUI();

            footerView.DrawGUI();
        }

        private IEnumerator LoadTextureInfos(IObserver<TextureInfo[]> observer)
        {
            var config = TextureViewerConfig.Instance;

            var ignoreFolderPaths = config.IgnoreFolderPaths;
            var ignoreFolderNames = config.IgnoreFolderNames;

            var infos = new List<TextureInfo>();

            var textureGuids = AssetDatabase.FindAssets("t:texture").ToArray();

            var index = 0;
            var total = textureGuids.Length;
            
            var chunk = textureGuids.Chunk(100).ToArray();

            Action displayProgress = () =>
            {
                EditorUtility.DisplayProgressBar("Progress", "Loading texture infos in project", (float)index / total);
            };

            foreach (var guids in chunk)
            {
                displayProgress.Invoke();

                foreach (var guid in guids)
                {
                    index++;

                    var assetPath = PathUtility.ConvertPathSeparator(AssetDatabase.GUIDToAssetPath(guid));

                    // Assets以外のファイル除外.
                    if (!assetPath.StartsWith(UnityEditorUtility.AssetsFolderName)) { continue; }

                    // 除外フォルダ以下のファイル除外.
                    if (ignoreFolderPaths.Any(y => assetPath.StartsWith(y))) { continue; }

                    // 除外フォルダ名を含むファイル除外.

                    var assetFolderPath = PathUtility.ConvertPathSeparator(Path.GetDirectoryName(assetPath));
                    var folders = assetFolderPath.Split(PathUtility.PathSeparator);

                    if (folders.Any(x => ignoreFolderNames.Contains(x))) { continue; }

                    // 生成.

                    var info = new TextureInfo(index, guid, assetPath);

                    // TextureImporterが取得できない場合は除外.
                
                    if (info.TextureImporter == null) { continue; }
                
                    // 追加.

                    infos.Add(info);
                }

                yield return null;
            }

            displayProgress.Invoke();

            observer.OnNext(infos.ToArray());
            observer.OnCompleted();
        }

        private IEnumerator LoadMainTextureBackground()
        {
            var chunk = textureInfos.Chunk(15).ToArray();

            var index = 0;
            var total = textureInfos.Length;

            foreach (var textureInfos in chunk)
            {
                var loadingText = string.Format("Loading Texture [{0} / {1}]", index, total);

                if (footerView != null)
                {
                    footerView.SetLoadingProgressText(loadingText);

                    Repaint();
                }

                if (initalLoading)
                {
                    EditorUtility.DisplayProgressBar("Progress", loadingText, (float)index / total);
                }

                foreach (var textureInfo in textureInfos)
                {
                    textureInfo.GetTexture();
                    index++;
                }

                yield return null;
            }

            if (initalLoading)
            {
                EditorUtility.ClearProgressBar();
            }

            if (footerView != null)
            {
                footerView.SetLoadingProgressText(null);
            }

            Repaint();
        }
        
        private TextureInfo[] GetTextureInfos(string searchText)
        {
            if (string.IsNullOrEmpty(searchText)) { return textureInfos; }

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            return textureInfos.Where(x => x.IsMatch(keywords)).ToArray();
        }
    }
}