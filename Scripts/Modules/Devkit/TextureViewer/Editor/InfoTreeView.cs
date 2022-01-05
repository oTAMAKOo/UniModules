
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;

namespace Modules.Devkit.TextureViewer
{
    public sealed class ColumnInfo
    {
        public string LabelName { get; set; }

        public float? FixedWidth { get; set; }

        public TextAlignment Alignment{ get; set; }

        public ColumnInfo(string labelName, float? fixedWidth = null)
        {
            LabelName = labelName;
            FixedWidth = fixedWidth;
            Alignment = TextAlignment.Center;
        }
    }

    public sealed class InfoTreeView : TreeView
    {
        //----- params -----

        public const string TextureNameLabel = "Name";

        private sealed class TextureInfoViewItem : TreeViewItem
        {
            public TextureInfo TextureInfo { get; private set; }

            public TextureInfoViewItem(TextureInfo textureInfo) : base(textureInfo.Id)
            {
                TextureInfo = textureInfo;
            }
        }

        //----- field -----

        private TextureInfoView textureInfoView = null;
        private CompressInfoView compressInfoView = null;

        private BuildTargetGroup platform = default;

        private DisplayMode? currentDisplayMode = null;

        private TextureInfo[] textureInfos = null;

        private Dictionary<string, float> columnWidthCache = null;
        
        private Vector2 scrollPosition = Vector2.zero;

        private int? sortedColumnIndex = null;

        private Func<int, bool, TextureInfo[], TextureInfo[]> sortCallback = null;

        private Subject<TextureInfo> onSelectionChanged = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        public InfoTreeView() : base(new TreeViewState()) { }

        public void Initialize(TextureInfo[] textureInfos, BuildTargetGroup platform, DisplayMode displayMode)
        {
            if (initialized) { return; }

            this.textureInfos = textureInfos;

            textureInfoView = new TextureInfoView();
            compressInfoView = new CompressInfoView();

            columnWidthCache = new Dictionary<string, float>();

            if (textureInfos.Any())
            {
                var maxTextureNameWidth = textureInfos.Max(x => x.GetNameWidth());

                columnWidthCache[TextureNameLabel] = maxTextureNameWidth + 30f;

                var maxFormatTextWidth = textureInfos.Max(x => x.GetFormatTextWidth(platform));

                columnWidthCache[CompressInfoView.TextureFormatLabel] = maxFormatTextWidth + 30f;
            }
            
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            
            SetPlatform(platform);
            SetDisplayMode(displayMode);

            initialized = true;
        }

        public void DrawGUI()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
                {
                    var controlRect = EditorGUILayout.GetControlRect(GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

                    OnGUI(controlRect);

                    scrollPosition = scrollView.scrollPosition;
                }
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { depth = -1 };

            var items = new List<TreeViewItem>();

            foreach (var textureInfo in textureInfos)
            {
                var item = new TextureInfoViewItem(textureInfo);

                items.Add(item);
            }

            root.children = items;

            return root;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override void DoubleClickedItem(int id)
        {   
            if (textureInfos == null){ return; }

            var textureInfo = textureInfos.FirstOrDefault(x => x.Id == id);
            
            if (textureInfo == null){ return; }

            var asset = AssetDatabase.LoadMainAssetAtPath(textureInfo.AssetPath);

            if (asset != null)
            {
                Selection.activeObject = asset;

                EditorGUIUtility.PingObject(asset);
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            if (selectedIds.IsEmpty()){ return; }

            if (textureInfos == null){ return; }

            var id = selectedIds.First();

            var textureInfo = textureInfos.FirstOrDefault(x => x.Id == id);
            
            if (textureInfo == null){ return; }

            if (onSelectionChanged != null)
            {
                onSelectionChanged.OnNext(textureInfo);
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = args.item as TextureInfoViewItem;

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                var rect = args.GetCellRect(visibleColumnIndex);

                var columnIndex = args.GetColumn(visibleColumnIndex);
                
                switch (currentDisplayMode)
                {
                    case DisplayMode.Texture:
                        textureInfoView.DrawRowGUI(rect, columnIndex, item.TextureInfo);
                        break;

                    case DisplayMode.Compress:
                        compressInfoView.DrawRowGUI(rect, columnIndex, item.TextureInfo);
                        break;
                }
            }
        }

        public void SetPlatform( BuildTargetGroup platform)
        {
            this.platform = platform;

            compressInfoView.Platform = platform;
        }

        public void SetDisplayMode(DisplayMode displayMode)
        {
            if (currentDisplayMode == displayMode) { return; }

            // �J�������X�V����O�ɕ���ۑ�. 
            if (multiColumnHeader != null)
            {
                foreach (var column in multiColumnHeader.state.columns)
                {
                    columnWidthCache[column.headerContent.text] = column.width;
                }
            }
            
            currentDisplayMode = displayMode;

            switch (displayMode)
            {
                case DisplayMode.Texture:
                    sortCallback = textureInfoView.Sort;
                    SetColumns(TextureInfoView.ColumnInfos);
                    break;

                case DisplayMode.Compress:
                    sortCallback = compressInfoView.Sort;
                    SetColumns(CompressInfoView.ColumnInfos);
                    break;
            }
        }

        private void SetColumns<T>(Dictionary<T, ColumnInfo> columnInfos) where T : Enum
        {
            var type = typeof(T);

            var elements = Enum.GetValues(type).Cast<T>();

            var columnCount = Enum.GetValues(type).Length;

            var columns = new MultiColumnHeaderState.Column[columnCount];

            foreach (var element in elements)
            {
                var index = Convert.ToInt32(element);

                var columnInfo = columnInfos.GetValueOrDefault(element);

                if (columnInfo == null){ continue; }

                var column = new MultiColumnHeaderState.Column();

                column.headerContent = new GUIContent(columnInfo.LabelName);

                if (columnWidthCache.ContainsKey(columnInfo.LabelName))
                {
                    column.width = columnWidthCache[columnInfo.LabelName];
                }
                else
                {
                    var size = MultiColumnHeader.DefaultStyles.columnHeader.CalcSize(column.headerContent);

                    column.width = size.x;
                }

                if (columnInfo.FixedWidth.HasValue)
                {
                    column.width = columnInfo.FixedWidth.Value;
                    column.minWidth = column.width;
                    column.maxWidth = column.width;
                }

                column.headerTextAlignment = TextAlignment.Center;
                column.autoResize = false;

                columns[index] = column;
            }

            multiColumnHeader = new MultiColumnHeader(new MultiColumnHeaderState(columns));

            multiColumnHeader.sortingChanged += OnSortingChanged;

            multiColumnHeader.ResizeToFit();

            Reload();
        }

        private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            if (sortCallback == null){ return; }

            var rows = GetRows();

            if (rows.Count <= 1){ return; }

            sortedColumnIndex = multiColumnHeader.sortedColumnIndex;

            if (!sortedColumnIndex.HasValue || sortedColumnIndex == -1) { return; }

            SetContents(textureInfos, true);

            Reload();
        }

        public void SetContents(TextureInfo[] textureInfos, bool sort)
        {
            this.textureInfos = textureInfos;

            if (sort && sortedColumnIndex.HasValue)
            {
                var ascending = multiColumnHeader.IsSortedAscending(sortedColumnIndex.Value);

                this.textureInfos = sortCallback.Invoke(multiColumnHeader.sortedColumnIndex, ascending, textureInfos);
            }

            if (textureInfos.Any())
            {
                var maxFormatTextWidth = textureInfos.Max(x => x.GetFormatTextWidth(platform));

                columnWidthCache[CompressInfoView.TextureFormatLabel] = maxFormatTextWidth + 30f;
            }

            Reload();
        }

        public void ResetSort()
        {
            sortedColumnIndex = null;
            textureInfos = textureInfos.OrderBy(x => x.Id).ToArray();

            SetContents(textureInfos, false);
        }

        public IObservable<TextureInfo> OnSelectionChangedAsObservable()
        {
            return onSelectionChanged ?? (onSelectionChanged = new Subject<TextureInfo>());
        }
    }
}