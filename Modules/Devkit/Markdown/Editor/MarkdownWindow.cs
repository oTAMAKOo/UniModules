
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using System.IO;

namespace Modules.Devkit.Markdown
{
    /// <summary>
    /// Markdownを整形して表示するウィンドウ.
    /// アセット毎に1つ開き、アセットの内容が更新されると表示も更新する.
    /// </summary>
    public sealed class MarkdownWindow : EditorWindow
    {
        //----- params -----

        private static readonly Vector2 DefaultWindowSize = new Vector2(860f, 900f);

        private static readonly Vector2 MinWindowSize = new Vector2(360f, 240f);

        private const float MaxContentWidth = 960f;

        private const long UpdateCheckInterval = 1000;

        //----- field -----

        [SerializeField]
        private string assetGuid = null;

        private Hash128 contentHash = default;

        private bool built = false;

        //----- property -----

        //----- method -----

        /// <summary> アセットを表示するウィンドウを開く. 表示中のウィンドウがあれば前面に出す. </summary>
        public static MarkdownWindow Open(TextAsset asset)
        {
            if (asset == null){ return null; }

            var assetPath = AssetDatabase.GetAssetPath(asset);

            var guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (string.IsNullOrEmpty(guid)){ return null; }

            var windows = Resources.FindObjectsOfTypeAll<MarkdownWindow>();

            foreach (var item in windows)
            {
                if (item.assetGuid != guid){ continue; }

                item.Show();
                item.Focus();

                return item;
            }

            var window = CreateInstance<MarkdownWindow>();

            window.assetGuid = guid;
            window.minSize = MinWindowSize;

            var position = window.position;

            position.size = DefaultWindowSize;

            window.position = position;

            window.Show();
            window.Build();

            return window;
        }

        // ウィンドウの生成時と、スクリプトの再読み込み後に呼ばれる.
        private void CreateGUI()
        {
            built = false;

            Build();
        }

        private void Build()
        {
            if (built){ return; }

            if (string.IsNullOrEmpty(assetGuid)){ return; }

            built = true;

            var root = rootVisualElement;

            root.Clear();

            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

            if (asset == null)
            {
                root.Add(new HelpBox("Asset not found.", HelpBoxMessageType.Warning));
                return;
            }

            // タイトルは、同名のファイルを区別できるように親フォルダ名を含める.

            var directoryName = Path.GetFileName(Path.GetDirectoryName(assetPath));

            titleContent = new GUIContent($"{directoryName}/{Path.GetFileName(assetPath)}");

            var markdownView = new MarkdownView();

            markdownView.style.width = Length.Percent(100);
            markdownView.style.maxWidth = MaxContentWidth;
            markdownView.style.alignSelf = Align.Center;

            markdownView.SetStyleSheet(MarkdownViewRules.LoadStyleSheet(MarkdownViewRules.Find(assetPath)));
            markdownView.SetMarkdown(asset.text, assetPath);

            var scrollView = new ScrollView(ScrollViewMode.Vertical);

            scrollView.style.flexGrow = 1;
            scrollView.Add(markdownView);

            root.Add(scrollView);

            contentHash = AssetDatabase.GetAssetDependencyHash(assetPath);

            // アセットの更新を定期的に確認する.
            root.schedule.Execute(() => UpdateContents(markdownView)).Every(UpdateCheckInterval);
        }

        private void UpdateContents(MarkdownView markdownView)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

            if (string.IsNullOrEmpty(assetPath)){ return; }

            var hash = AssetDatabase.GetAssetDependencyHash(assetPath);

            if (hash == contentHash){ return; }

            contentHash = hash;

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);

            if (asset == null){ return; }

            markdownView.SetMarkdown(asset.text, assetPath);
        }
    }
}
