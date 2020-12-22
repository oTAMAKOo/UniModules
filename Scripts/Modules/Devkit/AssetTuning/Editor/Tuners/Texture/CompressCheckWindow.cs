
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
    public sealed class CompressCheckWindow : SingletonEditorWindow<CompressCheckWindow>
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
            public bool compress;
            public Object folder;
            public TextureAssetInfo[] textureAssetInfos;
        }

        public sealed class TextureAssetInfo
        {
            public string textureGuid;
            public Vector2 size;
            public bool compress;
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

            titleContent = new GUIContent("CompressCheckWindow");
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
                    compress = false,
                    textureAssetInfos = new TextureAssetInfo[0],
                };

                compressFolderInfos.Add(compressFolderInfo);
            }

            var ignoreCompressFolderNames = config.IgnoreCompressFolderNames;

            var count = 0;
            var totalCount = guidsByFolderPath.SelectMany(x => x.Value).Count();

            foreach (var item in guidsByFolderPath)
            {
                var textureAssetInfos = new List<TextureAssetInfo>();

                var title = string.Format("Search Folder : {0}", item.Key);

                for (var i = 0; i < item.Value.Length; i++)
                {
                    var texturePath = AssetDatabase.GUIDToAssetPath(item.Value[i]);

                    var parts = texturePath.Substring(item.Key.Length).Split(PathUtility.PathSeparator);

                    if (parts.Any(x => ignoreCompressFolderNames.Contains(x))) { continue; }

                    EditorUtility.DisplayProgressBar(title, texturePath, (float)count++ / totalCount);

                    var info = BuildTextureAssetInfo(texturePath);

                    if (failedOnly)
                    {
                        if (!info.compress)
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
                    compressFolderInfo.compress = textureAssetInfos.All(x => x.compress);
                    compressFolderInfo.textureAssetInfos = textureAssetInfos.ToArray();
                }
            }

            EditorUtility.ClearProgressBar();

            return compressFolderInfos.ToArray();
        }

        private TextureAssetInfo BuildTextureAssetInfo(string assetPath)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            var compress = false;

            var isBlockCompress = Platforms.Any(x => textureImporter.IsBlockCompress(x));

            var size = textureImporter.GetPreImportTextureSize();

            if (isBlockCompress)
            {
                compress = IsMultipleOf4(size.x) && IsMultipleOf4(size.y);
            }

            var info = new TextureAssetInfo()
            {
                textureGuid = AssetDatabase.AssetPathToGUID(assetPath),
                size = size,
                compress = compress,
            };

            return info;
        }

        private TextureAssetInfo[] GetFilteredTextureAssetInfos()
        {
            if (!failedOnly) { return textureAssetInfos; }

            return textureAssetInfos.Where(x => !x.compress).ToArray();
        }

        private static bool IsMultipleOf4(float value)
        {
            return value % 4 == 0;
        }
    }

    public sealed class CompressFolderScrollView : EditorGUIFastScrollView<Object>
    {
        private GUIContent viewToolZoomIcon = null;
        private GUIContent vcscheckIcon = null;
        private GUIContent vcsdeleteIcon = null;
        private Subject<Object> onSearchRequest = null;

        public CompressCheckWindow.CompressFolderInfo[] CompressFolderInfos { get; set; }
        public CompressCheckWindow.AssetViewMode AssetViewMode { get; set; }

        public override Direction Type { get { return Direction.Vertical; } }
        
        protected override void DrawContent(int index, Object content)
        {
            if (viewToolZoomIcon == null)
            {
                viewToolZoomIcon = EditorGUIUtility.IconContent("ViewToolZoom");
            }

            if (vcscheckIcon == null)
            {
                vcscheckIcon = EditorGUIUtility.IconContent("vcs_check");
            }

            if (vcsdeleteIcon == null)
            {
                vcsdeleteIcon = EditorGUIUtility.IconContent("vcs_delete");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var info = CompressFolderInfos.FirstOrDefault(x => x.folder == content);

                if (info != null)
                {
                    var originLabelWidth = EditorLayoutTools.SetLabelWidth(12f);

                    var icon = info.compress ? vcscheckIcon : vcsdeleteIcon;

                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(12f)))
                    {
                        GUILayout.Space(-1f);

                        EditorGUILayout.LabelField(icon, GUILayout.Width(20f), GUILayout.Height(20f));
                    }

                    EditorLayoutTools.SetLabelWidth(originLabelWidth);
                }

                EditorGUI.BeginChangeCheck();

                switch (AssetViewMode)
                {
                    case CompressCheckWindow.AssetViewMode.Asset:
                        EditorGUILayout.ObjectField(content, typeof(Object), false);
                        break;

                    case CompressCheckWindow.AssetViewMode.Path:
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

    public sealed class TextureAssetInfoScrollView : EditorGUIFastScrollView<CompressCheckWindow.TextureAssetInfo>
    {
        private GUIStyle labelStyle = null;
        private GUIContent vcscheckIcon = null;
        private GUIContent vcsdeleteIcon = null;

        private ObjectCache<Texture> textureCache = null;

        public CompressCheckWindow.AssetViewMode AssetViewMode { get; set; }

        public override Direction Type { get { return Direction.Vertical; } }

        public TextureAssetInfoScrollView()
        {
            textureCache = new ObjectCache<Texture>();
        }

        protected override void DrawContent(int index, CompressCheckWindow.TextureAssetInfo content)
        {
            if (content == null) { return; }

            if (labelStyle == null)
            {
                labelStyle = GUI.skin.label;
                labelStyle.alignment = TextAnchor.MiddleLeft;
                labelStyle.wordWrap = false;
                labelStyle.stretchWidth = false;
            }

            if (vcscheckIcon == null)
            {
                vcscheckIcon = EditorGUIUtility.IconContent("vcs_check");
            }
            
            if (vcsdeleteIcon == null)
            {
                vcsdeleteIcon = EditorGUIUtility.IconContent("vcs_delete");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var originLabelWidth = EditorLayoutTools.SetLabelWidth(12f);

                var icon = content.compress ? vcscheckIcon : vcsdeleteIcon;

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(12f)))
                {
                    GUILayout.Space(-1f);

                    EditorGUILayout.LabelField(icon, GUILayout.Width(20f), GUILayout.Height(20f));
                }

                EditorLayoutTools.SetLabelWidth(originLabelWidth);

                var assetPath = AssetDatabase.GUIDToAssetPath(content.textureGuid);

                switch (AssetViewMode)
                {
                    case CompressCheckWindow.AssetViewMode.Asset:
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

                    case CompressCheckWindow.AssetViewMode.Path:
                        EditorGUILayout.SelectableLabel(assetPath, EditorStyles.textArea);
                        break;
                }

                EditorGUILayout.TextField(string.Format("{0}x{1}", content.size.x, content.size.y), labelStyle, GUILayout.Width(85f));
            }
        }
    }
}
