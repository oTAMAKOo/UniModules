
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Pinning
{
    public abstract class PinningWindow<T> : SingletonEditorWindow<T> where T : PinningWindow<T>
    {
        //----- params -----

        private const float HandleAreaWidth = 20f;
        private const float CancelButtonWidth = 16f;
        private const double DoubleClickThreshold = 0.3;

        [Serializable]
        protected sealed class PinnedItem
        {
            public Object target = null;

            public string comment = null;
        }

        //----- field -----

        [SerializeField]
        protected List<PinnedItem> pinning = null;

        private ReorderableList reorderableList = null;

        private GUIStyle labelStyle = null;
        private GUIStyle iconStyle = null;
        private GUIStyle cancelButtonStyle = null;

        private Vector2 scrollPosition = Vector2.zero;

        private PinnedItem commentInputTarget = null;

        private double lastClickTime = 0;
        private int lastClickIndex = -1;

        private int pressedIndex = -1;

        //----- property -----

        protected abstract string WindowTitle { get; }

        //----- method -----

        public void Initialize()
        {
            titleContent = new GUIContent(WindowTitle);

            pinning = new List<PinnedItem>();

            Load();

            Show(true);
        }

        void OnEnable()
        {
            labelStyle = null;
            iconStyle = null;
            cancelButtonStyle = null;

            Load();
        }

        void OnFocus()
        {
            commentInputTarget = null;
        }

        void OnLostFocus()
        {
            commentInputTarget = null;

            Repaint();
        }

        void OnGUI()
        {
            if (pinning == null){ return; }

            InitializeStyle();

            EnsureReorderableList();

            // コメント編集中は並び替えをロック.
            reorderableList.draggable = commentInputTarget == null;

            var e = Event.current;

            UpdatePinnedObject();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    Pin(Selection.activeObject);

                    commentInputTarget = null;
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(45f)))
                {
                    pinning.Clear();

                    commentInputTarget = null;

                    Save();
                }

                GUILayout.Space(4f);
            }

            GUILayout.Space(2f);

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                var originIconSize = EditorGUIUtility.GetIconSize();

                var iconSize = new Vector2(16f, 16f);

                EditorGUIUtility.SetIconSize(iconSize);

                reorderableList.DoLayoutList();

                EditorGUIUtility.SetIconSize(originIconSize);

                scrollPosition = scrollViewScope.scrollPosition;
            }

            // ウィンドウ全体でのDrag&Drop受付(外部から追加).
            HandleExternalDragAndDrop(e);

            // 要素外でMouseUpした場合のpressedIndexリセット漏れ対策.
            if (e.type == EventType.MouseUp && pressedIndex != -1)
            {
                pressedIndex = -1;
            }
        }

        private void EnsureReorderableList()
        {
            if (reorderableList != null && reorderableList.list == pinning){ return; }

            reorderableList = new ReorderableList(pinning, typeof(PinnedItem), true, false, false, false)
            {
                headerHeight = 0f,
                footerHeight = 0f,
                drawElementCallback = DrawElement,
                drawElementBackgroundCallback = DrawElementBackground,
                elementHeightCallback = GetElementHeight,
                onReorderCallback = OnReorder,
            };
        }

        private float GetElementHeight(int index)
        {
            return EditorGUIUtility.singleLineHeight + 2f;
        }

        private void DrawElementBackground(Rect rect, int index, bool isActive, bool isFocused)
        {
            // 選択時の青色を描画しないために isActive/isFocused を常に false で渡す.
            if (Event.current.type == EventType.Repaint)
            {
                ReorderableList.defaultBehaviours.DrawElementBackground(rect, index, false, false, true);
            }

            if (index < 0 || pinning.Count <= index){ return; }

            var item = pinning[index];

            if (item == null || item.target == null){ return; }

            // コメント編集中はTextFieldへのフォーカスを妨げない.
            if (item == commentInputTarget){ return; }

            // ハンドル領域(左端の≡)以外では並び替えを発動させない.
            var e = Event.current;

            if (e.button != 0){ return; }

            // MouseDrag: MouseDown後にマウスが動いたら外部へのDrag&Dropを開始(位置判定なし).
            if (e.type == EventType.MouseDrag && pressedIndex == index)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { item.target };
                DragAndDrop.StartDrag("Dragging");

                pressedIndex = -1;

                e.Use();

                return;
            }

            var contentRect = new Rect(rect.x + HandleAreaWidth, rect.y, rect.width - HandleAreaWidth, rect.height);

            if (!contentRect.Contains(e.mousePosition)){ return; }

            // 本体領域のMouseDownを先に消費してReorderableListの並び替えをブロック.
            if (e.type == EventType.MouseDown)
            {
                pressedIndex = index;

                e.Use();
            }
            else if (e.type == EventType.MouseUp && pressedIndex == index)
            {
                pressedIndex = -1;

                // 本体クリックとして扱う.
                var currentTime = EditorApplication.timeSinceStartup;
                var doubleClick = lastClickIndex == index && currentTime - lastClickTime < DoubleClickThreshold;

                lastClickTime = currentTime;
                lastClickIndex = index;

                OnMouseLeftDown(item.target, doubleClick);

                e.Use();
            }
        }

        private void DrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            if (index < 0 || pinning.Count <= index){ return; }

            var item = pinning[index];

            if (item == null || item.target == null){ return; }

            var thumbnail = (Texture)AssetPreview.GetMiniThumbnail(item.target);
            var toolTipText = GetToolTipText(item.target);
            var labelText = GetLabelName(item.target);

            var iconSize = new Vector2(16f, 16f);

            //------ icon ------

            // アイコンが見切れるのでサイズを補正.
            var iconRect = new Rect(rect.x, rect.y + 1f, iconSize.x + 2.5f, iconSize.y);

            var iconContent = new GUIContent(thumbnail, toolTipText);

            GUI.Label(iconRect, iconContent, iconStyle);

            //------ label ------

            var labelContent = new GUIContent(labelText, toolTipText);

            var labelMaxWidth = Mathf.Max(0f, rect.xMax - iconRect.xMax - 4f);

            var labelWidth = Mathf.Min(labelStyle.CalcSize(labelContent).x, labelMaxWidth);

            var labelRect = new Rect(iconRect.xMax + 2f, rect.y, labelWidth, rect.height);

            GUI.Label(labelRect, labelContent, labelStyle);

            //------ comment ------

            var commentX = labelRect.xMax + 4f;
            var commentRect = new Rect(commentX, rect.y, Mathf.Max(0f, rect.xMax - commentX), rect.height);

            if (item == commentInputTarget)
            {
                var textFieldWidth = Mathf.Max(0f, commentRect.width - CancelButtonWidth - 2f);

                var textFieldRect = new Rect(commentRect.x, commentRect.y, textFieldWidth, commentRect.height);
                var cancelButtonRect = new Rect(textFieldRect.xMax + 2f, commentRect.y, CancelButtonWidth, commentRect.height);

                EditorGUI.BeginChangeCheck();

                var comment = EditorGUI.DelayedTextField(textFieldRect, item.comment);

                if (EditorGUI.EndChangeCheck())
                {
                    item.comment = comment;

                    commentInputTarget = null;

                    Save();
                }

                if (GUI.Button(cancelButtonRect, "×", cancelButtonStyle))
                {
                    commentInputTarget = null;

                    GUI.FocusControl(null);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(item.comment))
                {
                    GUI.Label(commentRect, item.comment, EditorStyles.miniLabel);
                }
            }

            //------ right click menu ------

            HandleElementRightClick(rect, item);
        }

        private void HandleElementRightClick(Rect rect, PinnedItem item)
        {
            var e = Event.current;

            if (!rect.Contains(e.mousePosition)){ return; }

            if (e.type == EventType.MouseDown && e.button == 1)
            {
                ShowContextMenu(item);

                e.Use();
            }
        }

        private void OnReorder(ReorderableList list)
        {
            Save();

            // 選択状態の青い残像を解除.
            list.index = -1;

            Repaint();
        }

        private void ShowContextMenu(PinnedItem item)
        {
            var menu = new GenericMenu();

            void OnMenuCommentCommand()
            {
                commentInputTarget = item;
            }

            menu.AddItem(new GUIContent("Comment"), false, OnMenuCommentCommand);

            void OnMenuRemoveCommand()
            {
                if (commentInputTarget == item)
                {
                    commentInputTarget = null;
                }

                pinning.Remove(item);
                Save();
            }

            menu.AddItem(new GUIContent("Remove"), false, OnMenuRemoveCommand);

            menu.ShowAsContext();
        }

        private void HandleExternalDragAndDrop(Event e)
        {
            if (e.type != EventType.DragUpdated && e.type != EventType.DragPerform){ return; }

            var windowRect = new Rect(0f, 0f, position.width, position.height);

            if (!windowRect.Contains(e.mousePosition)){ return; }

            if (!DragAndDrop.objectReferences.Any()){ return; }

            var validate = ValidatePinned(DragAndDrop.objectReferences);

            DragAndDrop.visualMode = validate ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

            if (e.type == EventType.DragPerform && validate)
            {
                DragAndDrop.AcceptDrag();
                DragAndDrop.activeControlID = 0;

                Pin(DragAndDrop.objectReferences);
            }
        }

        private void InitializeStyle()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleLeft,
                };

                labelStyle.normal.textColor = EditorLayoutTools.DefaultContentColor;
                labelStyle.focused.textColor = EditorLayoutTools.DefaultContentColor;
            }

            if (iconStyle == null)
            {
                iconStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                };
            }

            if (cancelButtonStyle == null)
            {
                cancelButtonStyle = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                };
            }
        }

        private void Pin(Object target)
        {
            if (target == null){ return; }

            Pin(new[] { target });
        }

        private void Pin(Object[] targets)
        {
            foreach (var obj in targets)
            {
                // 既に登録済みの場合はスキップ(順序を変えない).
                if (pinning.Any(x => x.target == obj)){ continue; }

                var item = new PinnedItem() { target = obj };

                pinning.Add(item);
            }

            Save();

            Repaint();
        }

        protected virtual void UpdatePinnedObject()
        {
            // NullになったObjectの削除.
            for (var i = pinning.Count - 1; 0 <= i; --i)
            {
                if (pinning[i].target == null)
                {
                    pinning.RemoveAt(i);
                }
            }
        }

        protected abstract void Save();

        protected abstract void Load();

        protected abstract string GetToolTipText(Object item);

        protected abstract string GetLabelName(Object item);

        protected abstract bool ValidatePinned(Object[] items);

        protected abstract void OnMouseLeftDown(Object item, bool doubleClick);
    }
}
