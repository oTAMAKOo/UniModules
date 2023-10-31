
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions.Devkit;
using Extensions;
using Modules.Devkit.Prefs;

namespace Modules.TextData.Components
{
    public sealed class RecordView : TreeView
    {
        //----- params -----

        private sealed class Prefs
        {
            public static Dictionary<Column, float> columnWidth
            {
                get { return ProjectPrefs.Get<Dictionary<Column, float>>(typeof(Prefs).FullName + "-columnWidth", null); }
                set { ProjectPrefs.Set(typeof(Prefs).FullName + "-columnWidth", value); }
            }
        }

        private enum Column
        {
            Select,
            EnumName,
            Text,
            Identifier,
        }

        private const float RowHight = 22f;

        private const int ColumnWidthCheckInterval = 60;

        private sealed class ColumnInfo
        {
            public string Label { get; private set; }

            public bool FixedWidth  { get; private set; }

            public bool AllowToggleVisibility  { get; private set; }

            public ColumnInfo(string label, bool fixedWidth = true, bool allowToggleVisibility = false)
            {
                Label = label;
                FixedWidth = fixedWidth;
                AllowToggleVisibility = allowToggleVisibility;
            }
        }

        private sealed class RecordViewItem : TreeViewItem
        {
            public TextSelectData Record { get; private set; }

            public RecordViewItem(int index, TextSelectData record) : base(index)
            {
                Record = record;
            }
        }

        //----- field -----

        private GUIStyle guidLabelStyle = null;
        private GUIStyle enumNameLabelStyle = null;
        private GUIStyle contentTextStyle = null;

        private TextSelectData[] records = null;

        private int columnWidthCheckFrame = 0;

        private Column[] columnEnums = null;

        private Dictionary<Column, float> columnWidth = null;

        private Vector2 scrollPosition = Vector2.zero;

        private LifetimeDisposable lifetimeDisposable = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        public RecordView() : base(new TreeViewState()) { }

        public void Initialize()
        {
            if (initialized) { return; }

            lifetimeDisposable = new LifetimeDisposable();

            rowHeight = RowHight;
            showAlternatingRowBackgrounds = false;
            showBorder = true;

            columnEnums = Enum.GetValues(typeof(Column)).Cast<Column>().ToArray();

            SetHeaderColumns();

            initialized = true;
        }

        private void InitializeStyle()
        {
            if (guidLabelStyle == null)
            {
                guidLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                };

                guidLabelStyle.fontSize = (int)(guidLabelStyle.fontSize * 0.8f);
                guidLabelStyle.normal.textColor = EditorLayoutTools.DefaultContentColor;
            }

            if (enumNameLabelStyle == null)
            {
                enumNameLabelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                };

                enumNameLabelStyle.normal.textColor = EditorLayoutTools.DefaultContentColor;
            }

            if (contentTextStyle == null)
            {
                contentTextStyle = new GUIStyle(GUI.skin.textArea)
                {
                    alignment = TextAnchor.MiddleLeft,
                };

                contentTextStyle.normal.textColor = EditorLayoutTools.DefaultContentColor;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { depth = -1 };

            var items = new List<TreeViewItem>();

            if (records != null)
            {
                for (var i = 0; i < records.Length; i++)
                {
                    var item = new RecordViewItem(i, records[i]);

                    items.Add(item);
                }
            }

            root.children = items;

            return root;
        }

        public void SetRecords(TextSelectData[] records)
        {
            this.records = records;

            Reload();
        }

        private void SetHeaderColumns()
        {
            var columns = new MultiColumnHeaderState.Column[columnEnums.Length];

            columnWidth = Prefs.columnWidth;

            if (columnWidth == null)
            {
                columnWidth = new Dictionary<Column, float>()
                {
                    { Column.Select,     85 },
                    { Column.EnumName,   180 },
                    { Column.Text,       400 },
                    { Column.Identifier, 200 },
                };
            }

            var columnTable = new Dictionary<Column, ColumnInfo>()
            {
                { Column.Select,     new ColumnInfo(string.Empty) },
                { Column.EnumName,   new ColumnInfo("Enum", false) },
                { Column.Text,       new ColumnInfo("Text", false) },
                { Column.Identifier, new ColumnInfo("Identifier", false, true) },
            };

            foreach (var item in columnTable)
            {
                var column = new MultiColumnHeaderState.Column();

                var info = item.Value;

                var width = columnWidth[item.Key];

                column.headerContent = new GUIContent(info.Label);
                column.width = width;
                column.headerTextAlignment = TextAlignment.Center;
                column.canSort = false;
                column.autoResize = false;
                column.allowToggleVisibility = info.AllowToggleVisibility;

                if (info.FixedWidth)
                {
                    column.minWidth = width;
                    column.maxWidth = width;
                }

                columns[(int)item.Key] = column;
            }

            var columnHeader = new ColumnHeader(new MultiColumnHeaderState(columns));

            columnHeader.Initialize();

            columnHeader.OnChangeVisibilityAsObservable()
                .Subscribe(_ =>
                   {
                       RefreshCustomRowHeights();
                   })
                .AddTo(lifetimeDisposable.Disposable);

            multiColumnHeader = columnHeader;

            Reload();
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            // 行選択無効化.
            if (selectedIds.Any())
            {
                SetSelection(new List<int>());
            }
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem treeViewItem)
        {
            var item = treeViewItem as RecordViewItem;

            var hight = EditorLayoutTools.GetTextFieldHight(item.Record.Text);

            if (EditorLayoutTools.SingleLineHeight < hight)
            {
                hight += 4f;
            }

            return RowHight < hight ? hight : RowHight;
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

            if (ColumnWidthCheckInterval <= columnWidthCheckFrame++)
            {
                var changed = false;

                for (var i = 0; i < columnEnums.Length; i++)
                {
                    var columnEnum = columnEnums[i];
                    var column = multiColumnHeader.GetColumn(i);

                    if (columnWidth[columnEnum] != column.width)
                    {
                        columnWidth[columnEnum] = column.width;
                        changed = true;
                    }
                }

                if (changed)
                {
                    Prefs.columnWidth = columnWidth;
                }

                columnWidthCheckFrame = 0;
            }
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            TextSetter setter = null;

            if (TextSetterInspector.Current != null)
            {
                setter =  TextSetterInspector.Current.Instance;
            }

            InitializeStyle();

            var item = args.item as RecordViewItem;

            var record = item.Record;

            var columns = Enum.GetValues(typeof(Column)).Cast<Column>().ToArray();

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                var rect = args.GetCellRect(visibleColumnIndex);
                var columnIndex = args.GetColumn(visibleColumnIndex);

                var labelStyle = args.selected ? EditorStyles.whiteLabel : EditorStyles.label;

                labelStyle.alignment = TextAnchor.MiddleLeft;

                var highlight = setter != null && setter.TextGuid == record.TextGuid;

                using (new BackgroundColorScope(highlight ? new Color(0.7f, 0.9f, 0.95f) : new Color(0.95f, 0.95f, 0.95f)))
                {
                    var column = columns.ElementAt(columnIndex);

                    switch (column)
                    {
                        case Column.Select:
                            {
                                var setterInspector = TextSetterInspector.Current;

                                CenterRectUsingSingleLineHeight(ref rect);

                                rect.width -= 2f;
                                rect.height -= 2f;
                                rect.position += new Vector2(0f, -1f);

                                using (new DisableScope(setterInspector == null))
                                {
                                    if (GUI.Button(rect, "select", EditorStyles.miniButton))
                                    {
                                        UnityEditorUtility.RegisterUndo(setter);
                                    
                                        if (!string.IsNullOrEmpty(record.TextGuid))
                                        {
                                            if (setterInspector != null)
                                            {
                                                setterInspector.SetTextGuid(record.TextGuid);
                                                setterInspector.Repaint();
                                            }
                                        }
                                    }
                                }
                            }
                            break;

                        case Column.Identifier:
                            {
                                CenterRectUsingSingleLineHeight(ref rect);

                                rect.position += new Vector2(4f, 0f);

                                EditorGUI.SelectableLabel(rect, record.TextIdentifier, enumNameLabelStyle);
                            }
                            break;

                        case Column.EnumName:
                            {
                                CenterRectUsingSingleLineHeight(ref rect);

                                rect.position += new Vector2(4f, 0f);

                                EditorGUI.SelectableLabel(rect, record.Name, enumNameLabelStyle);
                            }
                            break;

                        case Column.Text:
                            {
                                rect.height -= 2f;

                                EditorGUI.SelectableLabel(rect, record.Text, contentTextStyle);
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(columnIndex), columnIndex, null);
                    }
                }
            }
        }

        private void DrawFieldGUI(Rect rect, TextSelectData record)
        {
            var setter = TextSetterInspector.Current.Instance;

            if (setter == null) { return; }

            var highlight = setter.TextGuid == record.TextGuid;

            var originBackgroundColor = GUI.backgroundColor;

            using (new BackgroundColorScope(highlight ? new Color(0.6f, 0.8f, 0.85f) : new Color(0.95f, 0.95f, 0.95f)))
            {
                var size = EditorStyles.label.CalcSize(new GUIContent(record.Text));

                size.y += 6f;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea, GUILayout.Height(size.y)))
                {
                    var labelStyle = new GUIStyle("IN TextField")
                    {
                        alignment = TextAnchor.MiddleLeft,
                    };

                    GUILayout.Space(10f);

                    GUILayout.Label(record.Name, labelStyle, GUILayout.MinWidth(220f), GUILayout.Height(size.y));

                    GUILayout.Label(record.Text, labelStyle, GUILayout.MaxWidth(500f), GUILayout.Height(size.y));

                    GUILayout.FlexibleSpace();

                    using (new EditorGUILayout.VerticalScope())
                    {
                        var buttonHeight = 16f;

                        GUILayout.Space((size.y - buttonHeight) * 0.5f);

                        using (new BackgroundColorScope(originBackgroundColor))
                        {
                            if (GUILayout.Button("select", EditorStyles.miniButton, GUILayout.Width(60f)))
                            {
                                UnityEditorUtility.RegisterUndo(setter);
                                
                                if (!string.IsNullOrEmpty(record.TextGuid))
                                {
                                    var setterInspector = TextSetterInspector.Current;

                                    if (setterInspector != null)
                                    {
                                        setterInspector.SetTextGuid(record.TextGuid);
                                        setterInspector.Repaint();
                                    }
                                }
                            }
                        }

                        GUILayout.Space((size.y - buttonHeight) * 0.5f);
                    }

                    GUILayout.Space(8f);
                }
            }
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        public void RefreshRowHeights()
        {
            RefreshCustomRowHeights();
        }
    }
}