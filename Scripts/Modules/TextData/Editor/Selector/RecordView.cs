
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.TextData.Components
{
    public sealed class RecordView : TreeView
    {
        //----- params -----

        private enum Column
        {
            Guid,
            EnumName,
            Text,
            Select,
        }

        private const float RowHight = 22f;

        private sealed class ColumnInfo
        {
            public string Label { get; private set; }

            public float Width { get; private set; }
            
            public bool FixedWidth  { get; private set; }

            public ColumnInfo(string label, float width, bool fixedWidth = true)
            {
                Label = label;
                Width = width;
                FixedWidth = fixedWidth;
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
            var columnCount = Enum.GetValues(typeof(Column)).Length;

            var columns = new MultiColumnHeaderState.Column[columnCount];

            var columnTable = new Dictionary<Column, ColumnInfo>()
            {
                { Column.Guid,     new ColumnInfo("Guid", 150, false) },
                { Column.EnumName, new ColumnInfo("Enum", 180, false) },
                { Column.Text,     new ColumnInfo("Text", 400, false) },
                { Column.Select,   new ColumnInfo(string.Empty, 85) },
            };

            foreach (var item in columnTable)
            {
                var column = new MultiColumnHeaderState.Column();

                var info = item.Value;

                column.headerContent = new GUIContent(info.Label);
                column.width = info.Width;
                column.headerTextAlignment = TextAlignment.Center;
                column.canSort = false;
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
                        case Column.Guid:
                            {
                                CenterRectUsingSingleLineHeight(ref rect);

                                rect.position += new Vector2(4f, 0f);

                                EditorGUI.SelectableLabel(rect, record.TextGuid, guidLabelStyle);
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