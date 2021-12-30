
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
            
            textureInfos = LoadTextureInfos();

            // バックグラウンド読み込み.
            Observable.FromMicroCoroutine(() => LoadMainTextureBackground())
                .Subscribe()
                .AddTo(Disposable);

            // View初期化.

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

            Show();

            initialized = true;
        }

        void OnGUI()
        {
            Initialize();
            
            toolbarView.DrawGUI();

            infoTreeView.DrawGUI();

            footerView.DrawGUI();
        }

        private TextureInfo[] LoadTextureInfos()
        {
            var config = TextureViewerConfig.Instance;

            var ignoreFolderPaths = config.IgnoreFolderPaths;
            var ignoreFolderNames = config.IgnoreFolderNames;

            var infos = new List<TextureInfo>();

            var assetPathByGuid = AssetDatabase.FindAssets("t:texture")
                .ToDictionary(x => x, x => PathUtility.ConvertPathSeparator(AssetDatabase.GUIDToAssetPath(x)));
                
            var targets = assetPathByGuid
                // Assets以下のファイル.
                .Where(x => x.Value.StartsWith(UnityEditorUtility.AssetsFolderName))
                // 除外フォルダ以下のファイルではない.
                .Where(x => ignoreFolderPaths.All(y => !x.Value.StartsWith(y)))
                // 除外フォルダ名を含まない.
                .Where(x =>
                   {
                       var assetFolder = Path.GetDirectoryName(x.Value);
                       var items = assetFolder.Split(PathUtility.PathSeparator);

                       return ignoreFolderNames.All(y => !items.Contains(y));
                   })
                .ToArray();

            var count = targets.Length;

            for (var i = 0; i < count; i++)
            {
                var guid = targets[i].Key;
                var assetPath = targets[i].Value;

                EditorUtility.DisplayProgressBar("progress", assetPath, (float)i / count);

                var info = new TextureInfo(i, guid, assetPath);
                
                if (info.TextureImporter == null) { continue; }

                infos.Add(info);
            }

            EditorUtility.ClearProgressBar();

            return infos.ToArray();
        }

        private IEnumerator LoadMainTextureBackground()
        {
            var chunk = textureInfos.Chunk(10).ToArray();

            foreach (var textureInfos in chunk)
            {
                foreach (var textureInfo in textureInfos)
                {
                    textureInfo.GetTexture();
                }

                yield return null;
            }
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