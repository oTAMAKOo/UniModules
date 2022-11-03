
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.MasterViewer
{
    public sealed class RecordView : TreeView
    {
        //----- params -----

        private static readonly Color EditedColor = new Color(0.2f, 1f, 1f, 1f);

        private const float RowHight = 21f;

        private sealed class RecordViewItem : TreeViewItem
        {
            public object Record { get; private set; }

            public RecordViewItem(int index, object record) : base(index)
            {
                Record = record;
            }
        }

        //----- field -----

        private MasterController masterController = null;

        private object[] records = null;

        private Vector2 scrollPosition = Vector2.zero;

        private Subject<Unit> onChangeRecord = null;

        private LifetimeDisposable lifetimeDisposable = null;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        public RecordView() : base(new TreeViewState()) { }

        public void Initialize(MasterController masterController)
        {
            if (initialized) { return; }

            this.masterController = masterController;

            lifetimeDisposable = new LifetimeDisposable();

            records = masterController.Records;

            rowHeight = RowHight;
            showAlternatingRowBackgrounds = false;
            showBorder = true;

            SetHeaderColumns();

            initialized = true;
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

        public void SetRecords(object[] records)
        {
            this.records = records;

            Reload();
        }

        private void SetHeaderColumns()
        {
            var valueNames = masterController.GetValueNames();

            var columns = new MultiColumnHeaderState.Column[valueNames.Length];

            for (var i = 0; i < valueNames.Length; i++)
            {
                var valueName = valueNames[i];
                var valueType = masterController.GetValueType(valueName);

                var labelContent = new GUIContent(valueName, valueType.GetFormattedName());
                var size = EditorStyles.label.CalcSize(labelContent);

                var column = new MultiColumnHeaderState.Column()
                {
                    headerContent = labelContent,
                    headerTextAlignment = TextAlignment.Center,
                    canSort = false,
                    autoResize = false,
                    width = size.x + 50f,
                };

                columns[i] = column;
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

            var valueNames = masterController.GetValueNames();
            
            var customRowHeight = 0f;
            
            for (var i = 0; i < valueNames.Length; i++)
            {
                var valueName = valueNames[i];

                if (!multiColumnHeader.IsColumnVisible(i)){ continue; }

                var valueType = masterController.GetValueType(valueName);

                if (valueType == typeof(string))
                {
                    var value = masterController.GetValue(item.Record, valueName) as string;

                    var hight = EditorRecordFieldUtility.GetTextFieldHight(value);

                    if (customRowHeight < hight)
                    {
                        customRowHeight = hight;

                        if (EditorGUIUtility.singleLineHeight < customRowHeight)
                        {
                            customRowHeight += 4f;
                        }
                    }
                }
            }

            return RowHight < customRowHeight ? customRowHeight : RowHight;
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
            var item = args.item as RecordViewItem;

            var valueNames = masterController.GetValueNames();

            for (var visibleColumnIndex = 0; visibleColumnIndex < args.GetNumVisibleColumns(); visibleColumnIndex++)
            {
                var rect = args.GetCellRect(visibleColumnIndex);

                var columnIndex = args.GetColumn(visibleColumnIndex);
                
                var valueName = valueNames.ElementAtOrDefault(columnIndex);

                if (string.IsNullOrEmpty(valueName)){ continue; }

                DrawFieldGUI(rect, valueName, item.Record);
            }
        }

        private void DrawFieldGUI(Rect rect, string valueName, object record)
        {
            var current = Event.current;

            var value = masterController.GetValue(record, valueName);

            var valueType = masterController.GetValueType(valueName);

            Action<object> onUpdateValue = x =>
            {
                masterController.UpdateValue(record, valueName, x);

                if (onChangeRecord != null)
                {
                    onChangeRecord.OnNext(Unit.Default);
                }

                RefreshCustomRowHeights();
            };

            var color = masterController.IsChanged(record, valueName) ? EditedColor : Color.white;

            rect.height = EditorGUIUtility.singleLineHeight;

            using (new BackgroundColorScope(color))
            {
                if (EditorRecordFieldUtility.IsArrayType(valueType))
                {
                    var builder = new StringBuilder();

                    if (value != null)
                    {
                        foreach (var item in (IEnumerable)value)
                        {
							builder.AppendFormat("{0},", item.GetType().IsClass ? item.ToJson() : item);
                        }
                    }

                    var text = builder.ToString().TrimEnd(',');

                    EditorGUI.LabelField(rect, text, EditorStyles.textField);

                    if (MasterController.CanEdit || !string.IsNullOrEmpty(text))
                    {
                        // メニュー表示と干渉するのでGUILayout.Buttonを使わない.
                        if (rect.Contains(current.mousePosition) && current.type == EventType.MouseDown && current.button == 0)
                        {
                            var mouseRect = new Rect(current.mousePosition, Vector2.one);

                            var arrayFieldPopupWindow = new ArrayFieldPopupWindow();

                            arrayFieldPopupWindow.SetContents(valueType, value);

                            arrayFieldPopupWindow.OnUpdateElementsAsObservable()
                                .Subscribe(x => onUpdateValue(x))
                                .AddTo(lifetimeDisposable.Disposable);

                            PopupWindow.Show(mouseRect, arrayFieldPopupWindow);

                            current.Use();
                        }
                    }
                }
                else
                {
                    if (valueType == typeof(string))
                    {
                        rect.height = EditorRecordFieldUtility.GetTextFieldHight(value as string);
                    }

                    EditorGUI.BeginChangeCheck();

                    try
                    {
                        value = EditorRecordFieldUtility.DrawField(rect, value, valueType);
                    }
                    catch (Exception e)
                    {
                        Debug.LogErrorFormat("Error: {0}\nValueName = {1}\nValueType = {2}\nValue = {3}\n", e.Message, valueName, valueType, value);
                    }

                    if (EditorGUI.EndChangeCheck() && MasterController.CanEdit)
                    {
                        onUpdateValue(value);
                    }
                }
            }

            // 右クリックでメニュー表示.
            if (rect.Contains(current.mousePosition) && current.type == EventType.MouseDown && current.button == 1)
            {
                if (masterController.IsChanged(record, valueName))
                {
                    var menu = new GenericMenu();

                    GenericMenu.MenuFunction onResetMenuClick = () =>
                    {
                        masterController.ResetValue(record, valueName);
                    };

                    menu.AddItem(new GUIContent("Reset"), false, onResetMenuClick);

                    menu.ShowAsContext();

                    GUI.FocusControl(string.Empty);

                    current.Use();
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

        public IObservable<Unit> OnChangeRecordAsObservable()
        {
            return onChangeRecord ?? (onChangeRecord = new Subject<Unit>());
        }
    }
}