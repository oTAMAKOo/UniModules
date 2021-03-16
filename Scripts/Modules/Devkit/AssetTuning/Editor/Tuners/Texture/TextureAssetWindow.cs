
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.ObjectCache;
using UniRx;

using Object = UnityEngine.Object;

namespace Modules.Devkit.AssetTuning
{
    public sealed class TextureAssetWindow : SingletonEditorWindow<TextureAssetWindow>
    {
        //----- params -----

        private static readonly BuildTargetGroup[] Platforms =
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.Standalone,
        };

        public enum ViewMode
        {
            SelectFolder,
            SearchResult,
        }

        public enum AssetViewMode
        {
            Asset,
            Path
        }

        public sealed class CompressFolderInfo
        {
            public bool hasWarning;
            public Object folder;
            public TextureAssetInfo[] textureAssetInfos;
        }

        public sealed class TextureAssetInfo
        {
            public string textureGuid;
            public Vector2 size;
            public string warning;
        }

        //----- field -----

        private ViewMode viewMode = ViewMode.SelectFolder;
        private AssetViewMode assetViewMode = AssetViewMode.Asset;
        private CompressFolderScrollView compressFolderScrollView = null;
        private TextureAssetInfoScrollView textureAssetInfoScrollView = null;
        private CompressFolderInfo[] compressFolderInfos = null;
        private TextureAssetInfo[] textureAssetInfos = null;
        private GUIContent tabprevIcon = null;
        private GUIContent viewToolZoomIcon = null;
        private bool failedOnly = true;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----
        
        public static void Open()
        {
            Instance.Initialize();
        }

        private void Initialize()
        {
            if (initialized) { return; }

            titleContent = new GUIContent("TextureAssetWindow");
            minSize = new Vector2(650f, 450f);

            var config = TextureAssetTunerConfig.Instance;

            viewMode = ViewMode.SelectFolder;
            assetViewMode = AssetViewMode.Asset;

            tabprevIcon = EditorGUIUtility.IconContent("tab_prev");
            viewToolZoomIcon = EditorGUIUtility.IconContent("ViewToolZoom");

            compressFolderInfos = new CompressFolderInfo[0];

            // Initialize search view.
            compressFolderScrollView = new CompressFolderScrollView();

            compressFolderScrollView.AssetViewMode = assetViewMode;

            compressFolderScrollView.OnSearchRequestAsObservable()
                .Subscribe(x =>
                    {
                        var infos = GetCompressFolderInfo(new Object[] { x });

                        if (infos.Any())
                        {
                            var info = infos.FirstOrDefault();
                            
                            textureAssetInfos = info.textureAssetInfos;
                            textureAssetInfoScrollView.Contents = GetFilteredTextureAssetInfos();
                            viewMode = ViewMode.SearchResult;
                            
                            var index = compressFolderInfos.IndexOf(y => y.folder == info.folder);

                            if (index != -1)
                            {
                                compressFolderInfos[index] = info;
                            }

                            Repaint();
                        }
                    })
                .AddTo(Disposable);

            compressFolderScrollView.Contents = config.CompressFolders;

            // Initialize result view.
            textureAssetInfoScrollView = new TextureAssetInfoScrollView();
            textureAssetInfoScrollView.AssetViewMode = assetViewMode;

            initialized = true;

            Show();
        }

        void OnGUI()
        {
            if (!initialized)
            {
                Initialize();
            }

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                var config = TextureAssetTunerConfig.Instance;

                if (GUILayout.Button(viewToolZoomIcon, EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
                {
                    var allFolders = config.CompressFolders;

                    compressFolderInfos = GetCompressFolderInfo(allFolders);

                    textureAssetInfos = null;
                    viewMode = ViewMode.SelectFolder;

                    Repaint();
                }

                EditorGUILayout.ObjectField(config, typeof(TextureAssetTunerConfig), false, GUILayout.Width(400f));

                if (viewMode == ViewMode.SearchResult)
                {
                    if (GUILayout.Button(tabprevIcon, EditorStyles.miniButton, GUILayout.Width(30f), GUILayout.Height(15f)))
                    {
                        viewMode = ViewMode.SelectFolder;
                        compressFolderScrollView.Contents = config.CompressFolders;
                    }
                }

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();

                assetViewMode = (AssetViewMode)EditorGUILayout.EnumPopup(assetViewMode, GUILayout.Width(60f));

                if (EditorGUI.EndChangeCheck())
                {
                    compressFolderScrollView.AssetViewMode = assetViewMode;
                    textureAssetInfoScrollView.AssetViewMode = assetViewMode;
                    Repaint();
                }

                var originLabelWidth = EditorLayoutTools.SetLabelWidth(70f);

                EditorGUI.BeginChangeCheck();

                failedOnly = EditorGUILayout.Toggle("Failed Only", failedOnly, GUILayout.Width(90f));

                if (EditorGUI.EndChangeCheck())
                {
                    if (viewMode == ViewMode.SearchResult)
                    {
                        textureAssetInfoScrollView.Contents = GetFilteredTextureAssetInfos();
                    }

                    Repaint();
                }

                EditorLayoutTools.SetLabelWidth(originLabelWidth);

                GUILayout.Space(5f);
            }

            EditorGUILayout.Separator();

            EditorLayoutTools.Title("Assets");

            switch (viewMode)
            {
                case ViewMode.SelectFolder:
                    compressFolderScrollView.CompressFolderInfos = compressFolderInfos;
                    compressFolderScrollView.Draw();
                    break;

                case ViewMode.SearchResult:
                    textureAssetInfoScrollView.Draw();
                    break;
            }
        }
        
        private CompressFolderInfo[] GetCompressFolderInfo(Object[] folders)
        {
            var config = TextureAssetTunerConfig.Instance;

            if (folders == null || folders.IsEmpty()) { return null; }

            var compressFolderInfos = new List<CompressFolderInfo>();

            var guidsByFolderPath = new Dictionary<string, string[]>();

            foreach (var folder in folders)
            {
                var path = AssetDatabase.GetAssetPath(folder);

                if (!AssetDatabase.IsValidFolder(path)) { return null; }

                var guids = AssetDatabase.FindAssets("t:texture", new string[] { path });

                guidsByFolderPath.Add(path, guids);

                var compressFolderInfo = new CompressFolderInfo()
                {
                    folder = folder,
                    hasWarning = false,
                    textureAssetInfos = new TextureAssetInfo[0],
                };

                compressFolderInfos.Add(compressFolderInfo);
            }

            var ignoreCompressFolders = config.IgnoreCompressFolders.Select(x => PathUtility.ConvertPathSeparator(x)).ToArray();

            var count = 0;
            var totalCount = guidsByFolderPath.SelectMany(x => x.Value).Count();

            foreach (var item in guidsByFolderPath)
            {
                var textureAssetInfos = new List<TextureAssetInfo>();

                var title = string.Format("Search Folder : {0}", item.Key);

                for (var i = 0; i < item.Value.Length; i++)
                {
                    var texturePath = AssetDatabase.GUIDToAssetPath(item.Value[i]);

                    var textureImporter = AssetImporter.GetAtPath(texturePath) as TextureImporter;

                    if (textureImporter == null){ continue; }

                    // フォルダパスで除外.

                    var ignoreFolderPaths = ignoreCompressFolders.Where(x => x.EndsWith(PathUtility.PathSeparator.ToString())).ToArray();

                    if (ignoreFolderPaths.Any(x => texturePath.Contains(x))) { continue; }

                    // フォルダ名で除外.

                    var parts = texturePath.Substring(item.Key.Length).Split(PathUtility.PathSeparator);

                    if (parts.Any(x => ignoreCompressFolders.Contains(x))) { continue; }

                    EditorUtility.DisplayProgressBar(title, texturePath, (float)count++ / totalCount);

                    var info = BuildTextureAssetInfo(texturePath);

                    if (failedOnly)
                    {
                        if (!string.IsNullOrEmpty(info.warning))
                        {
                            textureAssetInfos.Add(info);
                        }
                    }
                    else
                    {
                        textureAssetInfos.Add(info);
                    }
                }

                var compressFolderInfo = compressFolderInfos.FirstOrDefault(x => AssetDatabase.GetAssetPath(x.folder) == item.Key);

                if (compressFolderInfo != null)
                {
                    compressFolderInfo.hasWarning = textureAssetInfos.All(x => string.IsNullOrEmpty(x.warning));
                    compressFolderInfo.textureAssetInfos = textureAssetInfos.ToArray();
                }
            }

            EditorUtility.ClearProgressBar();

            return compressFolderInfos.ToArray();
        }

        private TextureAssetInfo BuildTextureAssetInfo(string assetPath)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            var size = textureImporter.GetPreImportTextureSize();

            var warning = textureImporter.GetImportWarning();

            var info = new TextureAssetInfo()
            {
                textureGuid = AssetDatabase.AssetPathToGUID(assetPath),
                size = size,
                warning = warning,
            };

            return info;
        }

        private TextureAssetInfo[] GetFilteredTextureAssetInfos()
        {
            if (!failedOnly) { return textureAssetInfos; }

            return textureAssetInfos.Where(x => !string.IsNullOrEmpty(x.warning)).ToArray();
        }
    }

    public sealed class CompressFolderScrollView : EditorGUIFastScrollView<Object>
    {
        private GUIContent viewToolZoomIcon = null;
        private GUIContent testPassedIcon = null;
        private GUIContent warnIcon = null;
        private Subject<Object> onSearchRequest = null;

        public TextureAssetWindow.CompressFolderInfo[] CompressFolderInfos { get; set; }
        public TextureAssetWindow.AssetViewMode AssetViewMode { get; set; }

        public override Direction Type { get { return Direction.Vertical; } }
        
        protected override void DrawContent(int index, Object content)
        {
            if (viewToolZoomIcon == null)
            {
                viewToolZoomIcon = EditorGUIUtility.IconContent("ViewToolZoom");
            }

            if (testPassedIcon == null)
            {
                testPassedIcon = EditorGUIUtility.IconContent("TestPassed");
            }

            if (warnIcon == null)
            {
                warnIcon = EditorGUIUtility.IconContent("Warning");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var info = CompressFolderInfos.FirstOrDefault(x => x.folder == content);

                if (info != null)
                {
                    var originLabelWidth = EditorLayoutTools.SetLabelWidth(12f);

                    var icon = info.hasWarning ? warnIcon : testPassedIcon;

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(12f)))
                    {
                        GUILayout.Space(4f);

                        EditorGUILayout.LabelField(icon, GUILayout.Width(18f), GUILayout.Height(16f));
                    }

                    EditorLayoutTools.SetLabelWidth(originLabelWidth);
                }

                EditorGUI.BeginChangeCheck();

                switch (AssetViewMode)
                {
                    case TextureAssetWindow.AssetViewMode.Asset:
                        EditorGUILayout.ObjectField(content, typeof(Object), false);
                        break;

                    case TextureAssetWindow.AssetViewMode.Path:
                        GUILayout.Label(AssetDatabase.GetAssetPath(content), EditorStyles.textArea);
                        break;
                }

                if (GUILayout.Button(viewToolZoomIcon, EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
                {
                    if (onSearchRequest != null)
                    {
                        onSearchRequest.OnNext(content);
                    }
                }
            }
        }

        public IObservable<Object> OnSearchRequestAsObservable()
        {
            return onSearchRequest ?? (onSearchRequest = new Subject<Object>());
        }
    }

    public sealed class TextureAssetInfoScrollView : EditorGUIFastScrollView<TextureAssetWindow.TextureAssetInfo>
    {
        private GUIStyle labelStyle = null;
        private GUIContent testPassedIcon = null;
        private GUIContent warnIcon = null;

        private ObjectCache<Texture> textureCache = null;

        public TextureAssetWindow.AssetViewMode AssetViewMode { get; set; }

        public override Direction Type { get { return Direction.Vertical; } }

        public TextureAssetInfoScrollView()
        {
            textureCache = new ObjectCache<Texture>();
        }

        protected override void DrawContent(int index, TextureAssetWindow.TextureAssetInfo content)
        {
            if (content == null) { return; }

            if (labelStyle == null)
            {
                labelStyle = GUI.skin.label;
                labelStyle.alignment = TextAnchor.MiddleLeft;
                labelStyle.wordWrap = false;
                labelStyle.stretchWidth = false;
            }

            if (testPassedIcon == null)
            {
                testPassedIcon = EditorGUIUtility.IconContent("TestPassed");
            }

            if (warnIcon == null)
            {
                warnIcon = EditorGUIUtility.IconContent("Warning");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var originLabelWidth = EditorLayoutTools.SetLabelWidth(12f);

                var icon = string.IsNullOrEmpty(content.warning) ? testPassedIcon : warnIcon;

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(12f)))
                {
                    GUILayout.Space(4f);

                    EditorGUILayout.LabelField(icon, GUILayout.Width(18f), GUILayout.Height(16f));
                }

                EditorLayoutTools.SetLabelWidth(originLabelWidth);

                var assetPath = AssetDatabase.GUIDToAssetPath(content.textureGuid);

                switch (AssetViewMode)
                {
                    case TextureAssetWindow.AssetViewMode.Asset:
                        {
                            Texture texture = null;

                            // レイアウト構築が終わってから表示する分だけ読み込む.
                            if (!IsLayoutUpdating)
                            {
                                texture = textureCache.Get(content.textureGuid);

                                if (texture == null)
                                {
                                    texture = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture;

                                    textureCache.Add(content.textureGuid, texture);
                                }
                            }

                            EditorGUILayout.ObjectField(texture, typeof(Texture), false);
                        }
                        break;

                    case TextureAssetWindow.AssetViewMode.Path:
                        EditorGUILayout.SelectableLabel(assetPath, EditorStyles.textArea);
                        break;
                }

                EditorGUILayout.TextField(string.Format("{0}x{1}", content.size.x, content.size.y), labelStyle, GUILayout.Width(85f));
            }
        }
    }
}
