﻿﻿
using UnityEngine;
using UnityEditor;
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

        [Serializable]
        protected sealed class PinnedItem
        {
            public Object target = null;

            public string comment = null;
        }

        //----- field -----

        [SerializeField]
        protected List<PinnedItem> pinning = null;

        private Vector2 scrollPosition = Vector2.zero;

        private PinnedItem commentInputTarget = null;

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

            var e = Event.current;

            UpdatePinnedObject();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbarButton))
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

                var requestSave = false;

                var labelStyle = new GUIStyle(GUI.skin.label);

                labelStyle.alignment = TextAnchor.MiddleLeft;

                EditorGUIUtility.SetIconSize(iconSize);

                for (var i = 0; i < pinning.Count; i++)
                {
                    var item = pinning[i];

                    if (item == null || item.target == null) { continue; }

                    var thumbnail = (Texture)AssetPreview.GetMiniThumbnail(item.target);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(3f);

                            //------ icon ------

                            var toolTipText = GetToolTipText(item.target);

                            var originLabelWidth = EditorGUIUtility.labelWidth;

                            EditorGUIUtility.labelWidth = 0f;

                            var iconContent = new GUIContent(thumbnail, toolTipText);
                            
                            // アイコンが見切れるのでサイズを補正. 
                            GUILayout.Label(iconContent, labelStyle, GUILayout.Width(iconSize.x + 3.5f));

                            EditorGUIUtility.labelWidth = originLabelWidth;

                            //------ label ------

                            var labelText = GetLabelName(item.target);

                            var labelContent = new GUIContent(labelText, toolTipText);

                            GUILayout.Label(labelContent, labelStyle, GUILayout.Height(18f));

                            //------ comment ------

                            if (item == commentInputTarget)
                            {
                                EditorGUI.BeginChangeCheck();

                                var comment = EditorGUILayout.DelayedTextField(item.comment);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    item.comment = comment;

                                    commentInputTarget = null;

                                    requestSave = true;
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(item.comment))
                                {
                                    EditorGUILayout.LabelField(item.comment, EditorStyles.miniLabel);
                                }
                            }
                        }

                        //------ mouse action ------

                        var rect = GUILayoutUtility.GetLastRect();

                        if (rect.Contains(e.mousePosition))
                        {
                            switch (e.button)
                            {
                                case 0:
                                    MouseLeftButton(rect, e, item);
                                    break;

                                case 1:
                                    MouseRightButton(rect, e, item);

                                    break;
                            }
                        }
                    }
                }

                if (requestSave)
                {
                    Save();
                }

                EditorGUIUtility.SetIconSize(originIconSize);

                scrollPosition = scrollViewScope.scrollPosition;

                // ドロップエリア
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:
                        {
                            var validate = ValidatePinned(DragAndDrop.objectReferences);

                            DragAndDrop.visualMode = validate ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

                            if (e.type == EventType.DragPerform)
                            {
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.activeControlID = 0;

                                if (validate)
                                {
                                    Pin(DragAndDrop.objectReferences);
                                }
                            }
                        }
                        break;
                }
            }
        }

        private void MouseLeftButton(Rect rect, Event e, PinnedItem item)
        {
            if (e.type == EventType.MouseDown)
            {
                OnMouseLeftDown(item.target, e.clickCount);
            }

            if (e.type == EventType.DragPerform || e.type == EventType.DragUpdated)
            {
                if (DragAndDrop.objectReferences.Any())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        DragAndDrop.activeControlID = 0;

                        // 中心より上なら上に挿入.
                        var toAbove = new Rect(rect.position, new Vector2(rect.width, rect.height * 0.5f))
                            .Contains(e.mousePosition);

                        Pin(DragAndDrop.objectReferences, item.target, toAbove);
                    }

                    e.Use();
                }
            }

            if (e.type == EventType.MouseDrag)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { item.target };
                DragAndDrop.StartDrag("Dragging");

                e.Use();
            }
        }

        private void MouseRightButton(Rect rect, Event e, PinnedItem item)
        {
            if (e.type == EventType.MouseDown)
            {
                var menu = new GenericMenu();

                GenericMenu.MenuFunction onMenuCommentCommand = () =>
                {
                    commentInputTarget = item;
                };

                menu.AddItem(new GUIContent("Comment"), false, onMenuCommentCommand);

                GenericMenu.MenuFunction onMenuRemoveCommand = () =>
                {
                    pinning.Remove(item);

                    Save();
                };

                menu.AddItem(new GUIContent("Remove"), false, onMenuRemoveCommand);

                menu.ShowAsContext();

                e.Use();
            }
        }

        private void Pin(Object target, Object adjacentObject = null, bool toAbove = false)
        {
            if(target == null) { return; }

            Pin(new[] { target }, adjacentObject, toAbove);
        }

        private void Pin(Object[] targets, Object adjacentObject = null, bool toAbove = false)
        {
            foreach (var obj in targets)
            {
                var item = pinning.FirstOrDefault(x => x.target == obj);

                if (item != null)
                {
                    pinning.Remove(item);
                }

                var newItem = new PinnedItem() { target = obj };

                if (adjacentObject != null && pinning.Any())
                {
                    var insertion = pinning.FindIndex(x => x.target == adjacentObject) + (toAbove ? 0 : 1);

                    var index = Mathf.Clamp(insertion, 0, pinning.Count);

                    pinning.Insert(index, newItem);
                }
                else
                {
                    pinning.Add(newItem);
                }
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

        protected abstract void OnMouseLeftDown(Object item, int clickCount);
    }
}
