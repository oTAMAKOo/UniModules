
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

        private Type masterType = null;

        private MasterController masterController = null;

        private RecordScrollView recordScrollView = null;

        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 mousDownPosition = Vector2.zero;

        private List<Rect> controlRects = null;
        private int focusedControl = -1;

        private float[] fieldWidth = null;

        private string searchText = null;

        private GUIContent lockedButtonContent = null;
        private GUIContent unLockedButtonContent = null;

        private LifetimeDisposable lifetimeDisposable = new LifetimeDisposable();

        //----- property -----

        //----- method -----

        public static RecordViewerWindow Open(MasterController masterController)
        {
            RecordViewerWindow recordViewerWindow = null;

            var windows = FindAllWindow();

            var window = windows.FirstOrDefault(x => x.masterType == masterController.MasterType);

            // 1つのマスターにつき1ウィンドウまでしか開かない.
            if (window == null)
            {
                window = CreateInstance<RecordViewerWindow>();
                
                window.Initialize(masterController);
                
                EditorApplication.delayCall += () =>
                {
                    window.SetWindowSize();
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

            masterType = masterController.MasterType;

            // フィールド幅計算.

            var valueNames = masterController.GetValueNames();

            fieldWidth = new float[valueNames.Length];

            for (var i = 0; i < valueNames.Length; i++)
            {
                var content = new GUIContent(valueNames[i]);
                var size = EditorStyles.label.CalcSize(content);
                
                fieldWidth[i] = Mathf.Max(80f, size.x + 20f);                
            }

            // レコード一覧View.

            recordScrollView = new RecordScrollView(masterController);
            recordScrollView.Contents = masterController.Records;
            recordScrollView.FieldWidth = fieldWidth;
            recordScrollView.HideHorizontalScrollBar = true;

            recordScrollView.OnRepaintRequestAsObservable()
                .Subscribe(_ => Repaint())
                .AddTo(lifetimeDisposable.Disposable);

            // アイコン.

            lockedButtonContent = EditorGUIUtility.IconContent("LockIcon");
            unLockedButtonContent = EditorGUIUtility.IconContent("LockIcon-On");
        }

        private void SetWindowSize()
        {
            var windowPosition = position;

            windowPosition.width = fieldWidth.Sum() + 50f;

            position = windowPosition;
        }

        void OnGUI()
        {
            if (masterController == null)
            {
                Close();
                return;
            }

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                // Toolbar.

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.Height(15f)))
                {
                    // 「編集」は実行中のみ.
                    if (Application.isPlaying)
                    {
                        // 「現在の状態」を表示する為、逆にする.
                        var buttonContent = recordScrollView.EnableEdit ? lockedButtonContent : unLockedButtonContent;

                        using (new ContentColorScope(EditorLayoutTools.DefaultContentColor))
                        {
                            recordScrollView.EnableEdit = GUILayout.Toggle(recordScrollView.EnableEdit, buttonContent, EditorStyles.toolbarButton, GUILayout.Width(20f));
                        }

                        GUILayout.Space(15f);
                    }

                    // 検索.

                    Action<string> onChangeSearchText = x =>
                    {
                        searchText = x;
                        recordScrollView.Contents = GetDisplayRecords();
                        Repaint();
                    };

                    Action onSearchCancel = () =>
                    {
                        searchText = string.Empty;
                        recordScrollView.Contents = GetDisplayRecords();
                        Repaint();
                    };

                    EditorLayoutTools.DrawToolbarSearchTextField(searchText, onChangeSearchText, onSearchCancel, GUILayout.Width(250f));             
                }

                // RecordView.

                var valueNames = masterController.GetValueNames();
            
                GUILayout.Space(3f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(3f);

                    controlRects.Clear();

                    for (var i = 0; i < valueNames.Length; i++)
                    {
                        var valueName = valueNames[i];

                        EditorGUILayout.LabelField(valueName, EditorStyles.miniButton, GUILayout.Width(fieldWidth[i]), GUILayout.Height(15f));

                        GetResizeHorizontalRect();
                    }

                    GUILayout.Space(3f);
                }

                GUILayout.Space(3f);

                using (new LabelWidthScope(0f))
                {
                    recordScrollView.Draw();
                }

                scrollPosition = scrollViewScope.scrollPosition;
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
                            y = mousDownPosition.y,
                        };

                        focusedControl = GetControlNum(controlPosition);

                        Event.current.Use();

                        Repaint();
                    }
                    break;

                case EventType.MouseDrag:
                    {
                        if (focusedControl == -1) { break; }

                        var diff = (int)(ev.mousePosition.x - mousDownPosition.x);

                        mousDownPosition = ev.mousePosition;

                        fieldWidth[focusedControl] = Mathf.Max(50f, fieldWidth[focusedControl] + diff);

                        recordScrollView.FieldWidth = fieldWidth;

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

        public float[] FieldWidth { get; set; }

        public bool EnableEdit { get; set; } = false;

        //----- method -----

        public RecordScrollView(MasterController masterController)
        {
            this.masterController = masterController;

            FieldWidth = new float[0];
        }

        protected override void DrawContent(int index, object content)
        {
            var valueNames = masterController.GetValueNames();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                for (var i = 0; i < valueNames.Length; i++)
                {
                    var valueName = valueNames[i];

                    var value = masterController.GetValue(content, valueName);

                    var valueType = masterController.GetValueType(valueName);

                    Action<object> onUpdateValue = x =>
                    {
                        if (EnableEdit)
                        {
                            masterController.UpdateValue(content, valueName, x);

                            if (onChangeRecord != null)
                            {
                                onChangeRecord.OnNext(Unit.Default);
                            }
                        }
                    };

                    var color = masterController.IsChanged(content, valueName) ? Color.yellow : Color.white;

                    using (new BackgroundColorScope(color))
                    {
                        var type = EditorRecordFieldUtility.GetDisplayType(valueType);

                        if (type.IsArray)
                        {
                            var builder = new StringBuilder();

                            foreach (var item in (IEnumerable)value)
                            {
                                builder.AppendFormat("{0},", item);
                            }

                            var text = builder.ToString();

                            text = text.TrimEnd(',');

                            if (GUILayout.Button(text, EditorStyles.textField, GUILayout.Width(FieldWidth[i])))
                            {
                                var mouseRect = new Rect(Event.current.mousePosition, Vector2.one);

                                var arrayFieldPopupWindow = new ArrayFieldPopupWindow();

                                var array = value as Array;

                                arrayFieldPopupWindow.SetContents(valueType, array.Cast<object>().ToArray());

                                arrayFieldPopupWindow.EnableEdit = EnableEdit;

                                arrayFieldPopupWindow.OnUpdateElementsAsObservable()
                                    .Subscribe(x => onUpdateValue(x))
                                    .AddTo(lifetimeDisposable.Disposable);

                                PopupWindow.Show(mouseRect, arrayFieldPopupWindow);
                            }
                        }
                        else
                        {
                            EditorGUI.BeginChangeCheck();

                            value = EditorRecordFieldUtility.DrawRecordField(value, valueType, GUILayout.Width(FieldWidth[i]));

                            if (EditorGUI.EndChangeCheck())
                            {
                                onUpdateValue(value);
                            }
                        }
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
