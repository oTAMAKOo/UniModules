
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.AssetBundleViewer
{
    public sealed class InfoTreeView : TreeView<int>
	{
		//----- params -----

		private enum Column
		{
			AssetBundleName,
			Group,
			Labels,
            AssetCount,
			Dependencies,
			FileSize,
            LoadFileSize,
		}

		private sealed class ColumnInfo
		{
			public string Label { get; private set; }

			public float Width { get; private set; }

			public bool FixedWidth { get; private set; }

			public ColumnInfo(string label, float width, bool fixedWidth = false)
			{
				Label = label;
				Width = width;
				FixedWidth = fixedWidth;
			}
		}

		private sealed class AssetBundleInfoViewItem : TreeViewItem<int>
		{
			public AssetBundleInfo AssetBundleInfo { get; private set; }

			public AssetBundleInfoViewItem(AssetBundleInfo assetBundleInfo) : base(assetBundleInfo.Id)
			{
				AssetBundleInfo = assetBundleInfo;
			}
		}

		//----- field -----

		private AssetBundleInfo[] assetBundleInfos = null;

		private AssetBundleInfo[] currentInfos = null;

		private IReadOnlyList<TreeViewItem<int>> currentItems = null;

		private Vector2 scrollPosition = Vector2.zero;

		private int? sortedColumnIndex = null;

		private string searchText = null;

        // Key: guid Value: AssetPath
        private Dictionary<string, string> assetPathCache = null;

		private Subject<AssetBundleInfo> onContentClick = null;

		[NonSerialized]
		private bool initialized = false;

		//----- property -----

		//----- method -----

		public InfoTreeView() : base(new TreeViewState<int>()) { }

		public void Initialize()
		{
			if (initialized) { return; }

			currentInfos = new AssetBundleInfo[0];
            assetPathCache = new Dictionary<string, string>();

			rowHeight = 20;
			showAlternatingRowBackgrounds = true;
			showBorder = true;

            SetColumns();

			initialized = true;
		}

		public void SetContents(AssetBundleInfo[] assetBundleInfos)
		{
			this.assetBundleInfos = assetBundleInfos;

            foreach (var assetBundleInfo in assetBundleInfos)
            {
                foreach (var guid in assetBundleInfo.AssetGuids)
                {
                    if (assetPathCache.ContainsKey(guid)){ continue; }
					
                    assetPathCache[guid] = AssetDatabase.GUIDToAssetPath(guid);
                }
            }

            currentInfos = GetDisplayContents();

            Reload();
		}

		public void SetColumns()
		{
			var columnCount = Enum.GetValues(typeof(Column)).Length;

			var columns = new MultiColumnHeaderState.Column[columnCount];

			var columnTable = new Dictionary<Column, ColumnInfo>()
			{
				{ Column.AssetBundleName, new ColumnInfo("AssetBundleName", 350) },
				{ Column.Group,			  new ColumnInfo("Group", 150) },
				{ Column.Labels,		  new ColumnInfo("Labels", 100) },
                { Column.AssetCount,	  new ColumnInfo("Assets", 80, true) },
                { Column.Dependencies,    new ColumnInfo("Dependencies", 100, true) },
				{ Column.FileSize,        new ColumnInfo("FileSize", 80) },
                { Column.LoadFileSize,	  new ColumnInfo("LoadFileSize", 100) },
            };

			foreach (var item in columnTable)
			{
				var column = new MultiColumnHeaderState.Column();

				var info = item.Value;

				column.headerContent = new GUIContent(info.Label);
				column.width = info.Width;
				column.headerTextAlignment = TextAlignment.Center;
				column.autoResize = false;

				if (info.FixedWidth)
				{
					column.minWidth = info.Width;
					column.maxWidth = info.Width;
				}

				columns[(int)item.Key] = column;
			}

			var columnHeader = new ColumnHeader(new MultiColumnHeaderState(columns));

			columnHeader.Initialize();

			multiColumnHeader = columnHeader;
			multiColumnHeader.sortingChanged += OnSortingChanged;
			multiColumnHeader.ResizeToFit();

			Reload();
		}

		public void DrawGUI()
		{
            DrawToolBar();

            DrawTreeView();
		}

        private void DrawToolBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
				GUILayout.FlexibleSpace();

				Action<string> onChangeSearchText = x =>
                {
                    searchText = x;

                    currentInfos = GetDisplayContents();
					
                    Reload();
                };

                Action onSearchCancel = () =>
                {
                    searchText = string.Empty;

                    currentInfos = GetDisplayContents();

					Reload();
                };

                EditorLayoutTools.DrawToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.MinWidth(300f));
            }
        }

        private void DrawTreeView()
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

        protected override TreeViewItem<int> BuildRoot()
		{
			var root = new TreeViewItem<int> { depth = -1 };

			var items = new List<TreeViewItem<int>>();

			for (var i = 0; i < currentInfos.Length; i++)
			{
				if (currentInfos[i] == null) { continue; }

				var item = new AssetBundleInfoViewItem(currentInfos[i]);

				items.Add(item);
			}

			currentItems = items;

			root.children = currentItems as List<TreeViewItem<int>>;

			return root;
		}

		protected override bool CanMultiSelect(TreeViewItem<int> item)
		{
			return false;
		}

		protected override void DoubleClickedItem(int id)
		{
			if (assetBundleInfos == null) { return; }

			var assetBundleInfo = assetBundleInfos.FirstOrDefault(x => x.Id == id);

			if (assetBundleInfo == null) { return; }

			if (onContentClick != null)
			{
				onContentClick.OnNext(assetBundleInfo);
			}
		}

		protected override void RowGUI(RowGUIArgs args)
		{
			var item = args.item as AssetBundleInfoViewItem;

			var info = item.AssetBundleInfo;

			var columns = Enum.GetValues(typeof(Column)).Cast<Column>().ToArray();

			for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
			{
				var rect = args.GetCellRect(visibleColumnIndex);
				var columnIndex = args.GetColumn(visibleColumnIndex);

				var labelStyle = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;

				labelStyle.alignment = TextAnchor.MiddleLeft;

				CenterRectUsingSingleLineHeight(ref rect);

				var column = columns.ElementAt(columnIndex);

				var value = string.Empty;

				switch (column)
				{
					case Column.AssetBundleName:
                        value = info.AssetBundleName;
						break;
                    case Column.Group:
                        value = info.Group;
						break;
                    case Column.Labels:
                        value = info.GetLabelsText();
						break;
                    case Column.AssetCount:
                        value = info.GetAssetCount().ToString();
                        break;
                    case Column.Dependencies:
                        value = info.GetDependenciesCount().ToString();
						break;
                    case Column.FileSize:
                        value = info.FileSize != 0 ? ByteDataUtility.GetBytesReadable(info.FileSize) : "---";
						break; 
                    case Column.LoadFileSize:
                        value = info.LoadFileSize != 0 ? ByteDataUtility.GetBytesReadable(info.LoadFileSize) : "---";
						break;
                    default:
						throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
				}

				if (!string.IsNullOrEmpty(value))
                {
                    EditorGUI.LabelField(rect, value, labelStyle);
                }
			}
		}

		private void OnSortingChanged(MultiColumnHeader multiColumnHeader)
		{
			var rows = GetRows();

			if (rows.Count <= 1) { return; }

			sortedColumnIndex = multiColumnHeader.sortedColumnIndex;

			if (!sortedColumnIndex.HasValue || sortedColumnIndex == -1) { return; }

			var ascending = multiColumnHeader.IsSortedAscending(sortedColumnIndex.Value);

			SortContents(multiColumnHeader.sortedColumnIndex, ascending);

			Reload();
		}

		private void SortContents(int sortedColumnIndex, bool ascending)
		{
			var columns = Enum.GetValues(typeof(Column)).Cast<Column>().ToArray();

			var column = columns.ElementAtOrDefault(sortedColumnIndex, Column.AssetBundleName);

			IOrderedEnumerable<AssetBundleInfo> orderedInfos = null;

			switch (column)
			{
				case Column.AssetBundleName:
					orderedInfos = currentInfos.Order(ascending, x => x.AssetBundleName);
					break;
				case Column.Group:
					orderedInfos = currentInfos.Order(ascending, x => x.Group, new NaturalComparer());
					break;
                case Column.Labels:
                    orderedInfos = currentInfos.Order(ascending, x => x.GetLabelsText());
                    break;
				case Column.AssetCount:
					orderedInfos = currentInfos.Order(ascending, x => x.GetAssetCount());
					break;
                case Column.Dependencies:
					orderedInfos = currentInfos.Order(ascending, x => x.GetDependenciesCount());
					break;
				case Column.FileSize:
					orderedInfos = currentInfos.Order(ascending, x => x.FileSize);
					break;
				case Column.LoadFileSize:
					orderedInfos = currentInfos.Order(ascending, x => x.LoadFileSize);
					break;
			}

			if (orderedInfos == null) { return; }

			currentInfos = orderedInfos.ThenBy(x => x.AssetBundleName, new NaturalComparer()).ToArray();
		}

        private AssetBundleInfo[] GetDisplayContents()
        {
            if (string.IsNullOrEmpty(searchText)) { return assetBundleInfos; }

            var targets = new List<AssetBundleInfo>();

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            foreach (var info in assetBundleInfos)
            {
                if (info.AssetBundleName.IsMatch(keywords))
                {
                    targets.Add(info);
                }
				else
                {
                    foreach (var assetGuid in info.AssetGuids)
                    {
                        var assetPath = assetPathCache.GetValueOrDefault(assetGuid);

                        if (assetPath.IsMatch(keywords))
                        {
                            targets.Add(info);
						    break;
                        }
                    }
                }
            }

            return targets.DistinctBy(x => x.AssetBundleName).ToArray();
        }

		public IObservable<AssetBundleInfo> OnContentClickAsObservable()
		{
			return onContentClick ?? (onContentClick = new Subject<AssetBundleInfo>());
		}
	}
}