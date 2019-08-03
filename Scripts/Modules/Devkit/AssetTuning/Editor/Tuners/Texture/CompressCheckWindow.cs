
using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;
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

        public sealed class TextureAssetInfo
        {
            public Texture textureAsset;
            public Vector2 size;
            public bool compress;
        }

        //----- field -----

        private ViewMode viewMode = ViewMode.SelectFolder;
        private AssetViewMode assetViewMode = AssetViewMode.Asset;
        private CompressFolderScrollView compressFolderScrollView = null;
        private TextureAssetInfoScrollView textureAssetInfoScrollView = null;
        private TextureAssetInfo[] textureAssetInfos = null;
        private Texture tabprevTexture = null;
        private bool failedOnly = false;

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

            var config = TextureAssetTunerConfig.Instance;

            viewMode = ViewMode.SelectFolder;
            assetViewMode = AssetViewMode.Asset;

            if (tabprevTexture == null)
            {
                tabprevTexture = EditorGUIUtility.FindTexture("tab_prev");
            }
            
            // Initialize search view.
            compressFolderScrollView = new CompressFolderScrollView();

            compressFolderScrollView.AssetViewMode = assetViewMode;

            compressFolderScrollView.OnSearchRequestAsObservable()
                .Subscribe(x =>
                    {
                        textureAssetInfos = FindTextureAssetInfoInFolder(x);
                        textureAssetInfoScrollView.Contents = GetFilteredTextureAssetInfos();
                        viewMode = ViewMode.SearchResult;
                        Repaint();
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
                if (viewMode == ViewMode.SearchResult)
                {
                    if (GUILayout.Button(new GUIContent(tabprevTexture), EditorStyles.miniButton, GUILayout.Width(30f), GUILayout.Height(15f)))
                    {
                        var config = TextureAssetTunerConfig.Instance;

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

            GUILayout.Space(3f);

            switch (viewMode)
            {
                case ViewMode.SelectFolder:
                    compressFolderScrollView.Draw();
                    break;

                case ViewMode.SearchResult:
                    textureAssetInfoScrollView.Draw();
                    break;
            }

            GUILayout.Space(1f);
        }
        
        private TextureAssetInfo[] FindTextureAssetInfoInFolder(Object folder)
        {
            if (folder == null) { return null; }

            var path = AssetDatabase.GetAssetPath(folder);

            if (!AssetDatabase.IsValidFolder(path)) { return null; }

            var list = new List<TextureAssetInfo>();
            
            var guids = AssetDatabase.FindAssets("t:texture", new string[] { path });

            var title = string.Format("Search Folder : {0}", path);

            for (var i = 0; i < guids.Length; i++)
            {
                var texturePath = AssetDatabase.GUIDToAssetPath(guids[i]);

                EditorUtility.DisplayProgressBar(title, texturePath, (float)i / guids.Length);

                var texture = AssetDatabase.LoadMainAssetAtPath(texturePath) as Texture;

                if (texture == null) { continue; }

                EditorUtility.DisplayProgressBar(title, texturePath, (float)i / guids.Length);

                var info = BuildTextureAssetInfo(texture);

                if (failedOnly)
                {
                    if (!info.compress)
                    {
                        list.Add(info);
                    }
                }
                else
                {
                    list.Add(info);
                }
            }

            EditorUtility.ClearProgressBar();

            return list.ToArray();
        }

        private TextureAssetInfo BuildTextureAssetInfo(Texture texture)
        {
            if (texture == null) { return null; }

            var assetPath = AssetDatabase.GetAssetPath(texture);

            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            var isCompressASTC = Platforms.Any(x => textureImporter.IsCompressASTC(x));

            if (!isCompressASTC) { return null; }

            var size = textureImporter.GetPreImportTextureSize();

            var compress = IsMultipleOf4(size.x) && IsMultipleOf4(size.y);

            var info = new TextureAssetInfo()
            {
                textureAsset = texture,
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
        private GUIContent viewToolZoomGUIContent = null;
        private Subject<Object> onSearchRequest = null;

        public CompressCheckWindow.AssetViewMode AssetViewMode { get; set; }

        public override Direction Type { get { return Direction.Vertical; } }

        protected override void DrawContent(int index, Object content)
        {
            if (viewToolZoomGUIContent == null)
            {
                var texture = EditorGUIUtility.FindTexture("ViewToolZoom");
                viewToolZoomGUIContent = new GUIContent(texture);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();

                switch (AssetViewMode)
                {
                    case CompressCheckWindow.AssetViewMode.Asset:
                        EditorGUILayout.ObjectField(content, typeof(Object), false);
                        break;

                    case CompressCheckWindow.AssetViewMode.Path:
                        GUILayout.Label(AssetDatabase.GetAssetPath(content), EditorLayoutTools.TextAreaStyle);
                        break;
                }

                if (GUILayout.Button(viewToolZoomGUIContent, EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
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
        private GUIContent vcscheckGUIContent = null;
        private GUIContent vcsdeleteGUIContent = null;

        public CompressCheckWindow.AssetViewMode AssetViewMode { get; set; }

        public override Direction Type { get { return Direction.Vertical; } }

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

            if (vcscheckGUIContent == null)
            {
                var texture = EditorGUIUtility.FindTexture("vcs_check");
                vcscheckGUIContent = new GUIContent(texture);
            }
            
            if (vcsdeleteGUIContent == null)
            {
                var texture = EditorGUIUtility.FindTexture("vcs_delete");
                vcsdeleteGUIContent = new GUIContent(texture);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                var originLabelWidth = EditorLayoutTools.SetLabelWidth(12f);

                var icon = content.compress ? vcscheckGUIContent : vcsdeleteGUIContent;

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(12f)))
                {
                    GUILayout.Space(-1f);

                    EditorGUILayout.LabelField(icon, GUILayout.Width(20f), GUILayout.Height(20f));
                }

                EditorLayoutTools.SetLabelWidth(originLabelWidth);

                switch (AssetViewMode)
                {
                    case CompressCheckWindow.AssetViewMode.Asset:
                        EditorGUILayout.ObjectField(content.textureAsset, typeof(Texture), false);
                        break;

                    case CompressCheckWindow.AssetViewMode.Path:
                        GUILayout.Label(AssetDatabase.GetAssetPath(content.textureAsset), EditorLayoutTools.TextAreaStyle);
                        break;
                }

                EditorGUILayout.TextField(string.Format("{0}x{1}", content.size.x, content.size.y), labelStyle, GUILayout.Width(85f));
            }
        }
    }
}
