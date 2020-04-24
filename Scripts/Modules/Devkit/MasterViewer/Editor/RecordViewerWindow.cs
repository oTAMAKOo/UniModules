
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

        private readonly Vector2 WindowSize = new Vector2(300f, 300f);

        //----- field -----
        
        private MasterController masterController = null;

        private RecordScrollView recordScrollView = null;

        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 mousDownPosition = Vector2.zero;

        private List<Rect> controlRects = null;
        private int focusedControl = -1;

        private string searchText = null;

        private LifetimeDisposable lifetimeDisposable = new LifetimeDisposable();

        //----- property -----

        //----- method -----

        public static RecordViewerWindow Open(MasterController masterController)
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
                    window.SetWindowPosition();
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

        public void Initialize(MasterController masterController)
        {
            this.masterController = masterController;

            titleContent = new GUIContent(masterController.GetDisplayMasterName());

            minSize = WindowSize;

            controlRects = new List<Rect>();

            // レコード一覧View.

            recordScrollView = new RecordScrollView(masterController)
            {
                Contents = masterController.Records,
                AlwaysShowVerticalScrollBar = true,
            };

            recordScrollView.OnRepaintRequestAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(lifetimeDisposable.Disposable);
        }

        private void SetWindowPosition()
        {
            var windowPosition = position;

            windowPosition.width = masterController.FieldWidth.Sum() + 50f;

            position = windowPosition;
        }

        void OnGUI()
        {
            if (masterController == null)
            {
                Close();
                return;
            }

            // Toolbar.

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
            {
                // 検索.

                Action<string> onChangeSearchText = x =>
                {
                    searchText = x;
                    recordScrollView.Contents = GetDisplayRecords();

                    EditorApplication.delayCall += () =>
                    {
                        Repaint();
                    };
                };

                Action onSearchCancel = () =>
                {
                    searchText = string.Empty;
                    recordScrollView.Contents = GetDisplayRecords();

                    EditorApplication.delayCall += () =>
                    {
                        Repaint();
                    };
                };

                EditorLayoutTools.DrawDelayedToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.Width(250f));
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

                    GUILayout.Space(3f);

                    GUILayout.Space(recordScrollView.VerticalScrollBarStyle.fixedWidth);
                }

                scrollPosition = new Vector2(recordScrollView.ScrollPosition.x, 0f);
            }

            GUILayout.Space(3f);

            using (new LabelWidthScope(0f))
            {
                recordScrollView.Draw();
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

        private object[] GetDisplayRecords()
        {
            if (string.IsNullOrEmpty(searchText)) { return masterController.Records; }

            var list = new List<object>();

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            foreach (var record in masterController.Records)
            {
                var valueNames = masterController.GetValueNames();

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

            return list.ToArray();
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

        private MasterController masterController = null;

        private Subject<Unit> onChangeRecord = null;

        private LifetimeDisposable lifetimeDisposable = new LifetimeDisposable();

        //----- property -----

        public override Direction Type { get { return Direction.Vertical; } }

        //----- method -----

        public RecordScrollView(MasterController masterController)
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
