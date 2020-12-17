
using UnityEngine;
using UnityEditor;
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
    public sealed class RecordViewerWindow : EditorWindow
    {
        //----- params -----


        private const int PageCellCapacity = 1000;

        //----- field -----

        private MasterControllerBase masterController = null;

        private RecordScrollView recordScrollView = null;

        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 mousDownPosition = Vector2.zero;

        private List<Rect> controlRects = null;
        private int focusedControl = -1;

        private int page = 0;
        private List<object[]> pageRecords = null;

        private string searchText = null;
        private object[] searchedRecords = null;

        private bool requestMaxWidthUpdate = false;

        private GUIStyle pagingTextFieldStyle = null;
        private GUIContent prevArrowIcon = null;
        private GUIContent nextArrowIcon = null;

        private LifetimeDisposable lifetimeDisposable = new LifetimeDisposable();

        //----- property -----

        //----- method -----

        public static RecordViewerWindow Open(MasterControllerBase masterController)
        {
            RecordViewerWindow recordViewerWindow = null;

            var windows = FindAllWindow();

            var window = windows.FirstOrDefault(x => x.masterController.MasterType == masterController.MasterType);

            // 1つのマスターにつき1ウィンドウまでしか開かない.
            if (window == null)
            {
                window = CreateInstance<RecordViewerWindow>();

                window.Initialize(masterController);

                EditorApplication.delayCall += () =>
                {
                    window.requestMaxWidthUpdate = true;
                    window.Show();
                };

                recordViewerWindow = window;
            }

            window.Focus();

            return recordViewerWindow;
        }

        public static RecordViewerWindow[] FindAllWindow()
        {
            return (RecordViewerWindow[])Resources.FindObjectsOfTypeAll(typeof(RecordViewerWindow));
        }

        public void Initialize(MasterControllerBase masterController)
        {
            this.masterController = masterController;

            titleContent = new GUIContent(masterController.GetDisplayMasterName());

            controlRects = new List<Rect>();

            page = 0;
            searchText = string.Empty;

            UpdatePageRecords(masterController.Records);

            // レコード一覧View.

            recordScrollView = new RecordScrollView(masterController)
            {
                Contents = GetDisplayRecords(),
                AlwaysShowVerticalScrollBar = true,
            };

            recordScrollView.OnRepaintRequestAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(lifetimeDisposable.Disposable);

            // Style.

            pagingTextFieldStyle = new GUIStyle(EditorStyles.toolbarTextField)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
            };

            // アイコン.

            prevArrowIcon = EditorGUIUtility.IconContent("Profiler.PrevFrame");
            nextArrowIcon = EditorGUIUtility.IconContent("Profiler.NextFrame");

            // ウィンドウ最大幅更新.        
            requestMaxWidthUpdate = true;
        }

        void OnGUI()
        {
            if (masterController == null)
            {
                Close();
                return;
            }

            if (requestMaxWidthUpdate)
            {
                UpdateMaxWidth();
            }

            // Toolbar.

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                // 検索.

                Action<string> onChangeSearchText = x =>
                {
                    searchText = x;
                    page = 0;

                    recordScrollView.Contents = GetDisplayRecords();

                    EditorApplication.delayCall += () =>
                    {
                        Repaint();
                    };
                };

                Action onSearchCancel = () =>
                {
                    searchText = string.Empty;
                    page = 0;

                    recordScrollView.Contents = GetDisplayRecords();

                    EditorApplication.delayCall += () =>
                    {
                        Repaint();
                    };
                };

                EditorLayoutTools.DrawDelayedToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.Width(250f));

                GUILayout.FlexibleSpace();

                // ページング.

                var pageCount = pageRecords.Count;

                if (1 < pageCount)
                {
                    using (new DisableScope(page <= 0))
                    {
                        if (GUILayout.Button(prevArrowIcon, EditorStyles.toolbarButton))
                        {
                            page--;

                            recordScrollView.Contents = GetDisplayRecords();
                        }
                    }

                    EditorGUI.BeginChangeCheck();

                    var newPage = EditorGUILayout.DelayedIntField(string.Empty, page, pagingTextFieldStyle, GUILayout.Width(40f));

                    if (EditorGUI.EndChangeCheck())
                    {
                        page = Mathf.Clamp(newPage, 0, pageCount - 1);

                        recordScrollView.Contents = GetDisplayRecords();
                    }

                    using (new DisableScope(pageCount - 1 <= page))
                    {
                        if (GUILayout.Button(nextArrowIcon, EditorStyles.toolbarButton))
                        {
                            page++;

                            recordScrollView.Contents = GetDisplayRecords();
                        }
                    }
                }
            }

            var scrollBaseRect = GUILayoutUtility.GetLastRect();

            // RecordView.

            var valueNames = masterController.GetValueNames();

            GUILayout.Space(3f);

            using (new EditorGUILayout.ScrollViewScope(scrollPosition, false, false, GUIStyle.none, GUIStyle.none, GUIStyle.none))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(3f);

                    controlRects.Clear();

                    for (var i = 0; i < valueNames.Length; i++)
                    {
                        var valueName = valueNames[i];

                        var fieldWidth = masterController.FieldWidth[i];

                        EditorGUILayout.LabelField(valueName, EditorStyles.miniButton, GUILayout.Width(fieldWidth), GUILayout.Height(15f));

                        GetResizeHorizontalRect();
                    }

                    GUILayout.Space(10f);

                    var verticalScrollBarStyle = recordScrollView.GetVerticalScrollBarStyle();

                    if (verticalScrollBarStyle != null)
                    {
                        GUILayout.Space(verticalScrollBarStyle.fixedWidth);
                    }
                }

                scrollPosition = new Vector2(recordScrollView.ScrollPosition.x, 0f);
            }

            GUILayout.Space(3f);

            using (new LabelWidthScope(0f))
            {
                recordScrollView.Draw(true, GUILayout.MinHeight(position.height - 45f));
            }

            // Event Handling.

            var ev = Event.current;

            switch (ev.type)
            {
                case EventType.MouseUp:
                    {
                        focusedControl = -1;

                        Event.current.Use();

                        Repaint();
                    }
                    break;

                case EventType.MouseDown:
                    {
                        mousDownPosition = ev.mousePosition;

                        var controlPosition = new Vector2()
                        {
                            x = scrollPosition.x + mousDownPosition.x,
                            y = mousDownPosition.y - (scrollBaseRect.y + scrollBaseRect.height),
                        };

                        focusedControl = GetControlNum(controlPosition);

                        GUI.FocusControl(string.Empty);

                        Event.current.Use();

                        Repaint();
                    }
                    break;

                case EventType.MouseDrag:
                    {
                        if (focusedControl == -1) { break; }

                        var diff = (int)(ev.mousePosition.x - mousDownPosition.x);

                        mousDownPosition = ev.mousePosition;

                        masterController.FieldWidth[focusedControl] = Mathf.Max(50f, masterController.FieldWidth[focusedControl] + diff);

                        requestMaxWidthUpdate = true;

                        Repaint();
                    }
                    break;
            }
        }

        private void GetResizeHorizontalRect()
        {
            var resizeRectSize = new Vector2(20f, 10f);

            var rect = GUILayoutUtility.GetLastRect();

            rect.x = rect.x + rect.width - resizeRectSize.x * 0.5f;
            rect.y -= resizeRectSize.y * 0.5f;

            rect.width = resizeRectSize.x;
            rect.height += resizeRectSize.y;

            controlRects.Add(rect);

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.ResizeHorizontal);
        }

        private int GetControlNum(Vector2 pos)
        {
            for (var i = 0; i < controlRects.Count; i++)
            {
                if (controlRects[i].Contains(pos))
                {
                    return i;
                }
            }

            return -1;
        }

        private void UpdatePageRecords(object[] records)
        {
            var valueNames = masterController.GetValueNames();

            var recordCellCount = valueNames.Length;

            var cellCount = 0;

            var list = new List<object>();

            pageRecords = new List<object[]>();

            for (var i = 0; i < records.Length; i++)
            {
                list.Add(records[i]);

                cellCount += recordCellCount;

                if (PageCellCapacity <= cellCount)
                {
                    pageRecords.Add(list.ToArray());

                    cellCount = 0;
                    list.Clear();
                }
            }

            pageRecords.Add(list.ToArray());
        }

        private void UpdateSearchedRecords()
        {
            searchedRecords = null;

            // 検索テキストでフィルタ.

            if (string.IsNullOrEmpty(searchText)) { return; }

            var list = new List<object>();

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            var records = masterController.Records;

            var valueNames = masterController.GetValueNames();

            foreach (var record in records)
            {
                foreach (var valueName in valueNames)
                {
                    var value = masterController.GetValue(record, valueName);

                    if (value != null)
                    {
                        // or 検索なのでIsMatchを使わない.
                        if (keywords.Any(x => value.ToString().Contains(x)))
                        {
                            list.Add(record);
                            break;
                        }
                    }
                }
            }

            searchedRecords = list.ToArray();
        }


        private object[] GetDisplayRecords()
        {
            var records = masterController.Records;

            // 検索.

            UpdateSearchedRecords();

            if (searchedRecords != null)
            {
                records = searchedRecords;
            }

            // ページング.

            UpdatePageRecords(records);

            records = pageRecords.ElementAtOrDefault(page);

            return records;
        }

        private void UpdateMaxWidth()
        {
            if (!requestMaxWidthUpdate) { return; }

            var maxWidth = 0f;

            var valueNames = masterController.GetValueNames();

            for (var i = 0; i < valueNames.Length; i++)
            {
                maxWidth += masterController.FieldWidth[i] + 4f;
            }

            var verticalScrollBarStyle = recordScrollView.GetVerticalScrollBarStyle();

            if (verticalScrollBarStyle != null)
            {
                maxWidth += verticalScrollBarStyle.fixedWidth;
            }
            
            maxSize = new Vector2(maxWidth, maxSize.y);

            requestMaxWidthUpdate = false;

            Repaint();
        }

        public IObservable<Unit> OnChangeRecordAsObservable()
        {
            return recordScrollView.OnChangeRecordAsObservable();
        }
    }

    public sealed class RecordScrollView : EditorGUIFastScrollView<object>
    {
        //----- params -----

        //----- field -----

        private MasterControllerBase masterController = null;

        private Subject<Unit> onChangeRecord = null;

        private LifetimeDisposable lifetimeDisposable = new LifetimeDisposable();

        //----- property -----

        public override Direction Type { get { return Direction.Vertical; } }

        //----- method -----

        public RecordScrollView(MasterControllerBase masterController)
        {
            this.masterController = masterController;
        }

        protected override void DrawContent(int index, object content)
        {
            var current = Event.current;

            var valueNames = masterController.GetValueNames();

            var fieldAreaInfos = new List<Tuple<Rect, string>>();

            using (new EditorGUILayout.HorizontalScope())
            {
                for (var i = 0; i < valueNames.Length; i++)
                {
                    var valueName = valueNames[i];

                    var value = masterController.GetValue(content, valueName);

                    var valueType = masterController.GetValueType(valueName);

                    Action<object> onUpdateValue = x =>
                    {
                        masterController.UpdateValue(content, valueName, x);

                        if (onChangeRecord != null)
                        {
                            onChangeRecord.OnNext(Unit.Default);
                        }
                    };

                    var color = masterController.IsChanged(content, valueName) ? Color.yellow : Color.white;

                    var fieldWidth = masterController.FieldWidth[i];

                    using (new BackgroundColorScope(color))
                    {
                        if (EditorRecordFieldUtility.IsArrayType(valueType))
                        {
                            var builder = new StringBuilder();

                            if (value != null)
                            {
                                foreach (var item in (IEnumerable)value)
                                {
                                    builder.AppendFormat("{0},", item);
                                }
                            }

                            var text = builder.ToString().TrimEnd(',');

                            GUILayout.Label(text, EditorStyles.textField, GUILayout.Width(fieldWidth));

                            var fieldRect = GUILayoutUtility.GetLastRect();

                            // メニュー表示と干渉するのでGUILayout.Buttonを使わない.
                            if (fieldRect.Contains(current.mousePosition) && current.type == EventType.MouseDown && current.button == 0)
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
                        else
                        {
                            EditorGUI.BeginChangeCheck();

                            try
                            {
                                value = EditorRecordFieldUtility.DrawRecordField(value, valueType, GUILayout.Width(fieldWidth));
                            }
                            catch (Exception e)
                            {
                                Debug.LogErrorFormat("Error: {0}\nValueName = {1}\nValueType = {2}\nValue = {3}\n", e.Message, valueName, valueType, value);
                            }

                            if (EditorGUI.EndChangeCheck())
                            {
                                onUpdateValue(value);
                            }
                        }

                        fieldAreaInfos.Add(Tuple.Create(GUILayoutUtility.GetLastRect(), valueName));
                    }
                }
            }

            // 右クリックでメニュー表示.
            if (current.type == EventType.MouseDown && current.button == 1)
            {
                var fieldAreaInfo = fieldAreaInfos.FirstOrDefault(x => x.Item1.Contains(current.mousePosition));

                if (fieldAreaInfo != null)
                {
                    var valueName = fieldAreaInfo.Item2;

                    if (masterController.IsChanged(content, valueName))
                    {
                        var menu = new GenericMenu();

                        GenericMenu.MenuFunction onResetMenuClick = () =>
                        {
                            masterController.ResetValue(content, valueName);
                        };

                        menu.AddItem(new GUIContent("Reset"), false, onResetMenuClick);

                        menu.ShowAsContext();

                        current.Use();
                    }
                }
            }
        }

        public IObservable<Unit> OnChangeRecordAsObservable()
        {
            return onChangeRecord ?? (onChangeRecord = new Subject<Unit>());
        }
    }
}
