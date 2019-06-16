﻿﻿
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Extensions.Devkit;
using UniRx;

namespace Modules.Devkit.Pinning
{
	public abstract class PinningWindow<T> : SingletonEditorWindow<T> where T : PinningWindow<T>
    {
        //----- params -----

        //----- field -----

        [SerializeField]
        protected List<Object> pinnedObject = null;

        private Vector2 scrollPosition = Vector2.zero;

        //----- property -----

        protected abstract string WindowTitle { get; }
        protected abstract string PinnedPrefsKey { get; }

        //----- method -----

        public virtual void Initialize()
        {
            titleContent = new GUIContent(WindowTitle);

            pinnedObject = new List<Object>();

            Load();

            Show(true);
        }

        void OnEnable()
        {
            Load();
        }

        void OnGUI()
        {
            var e = Event.current;

            UpdatePinnedObject();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbarButton))
            {
                if (GUILayout.Button("+", EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    Pin(Selection.activeObject);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(45f)))
                {
                    pinnedObject.Clear();
                    Save();
                }

                GUILayout.Space(4f);
            }

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                var originIconSize = EditorGUIUtility.GetIconSize();

                var iconSize = new Vector2(16f, 16f);

                var labelStyle = GUI.skin.label;
                labelStyle.alignment = TextAnchor.MiddleLeft;

                EditorGUIUtility.SetIconSize(iconSize);

                for (var i = 0; i < pinnedObject.Count; i++)
                {
                    var item = pinnedObject[i];

                    if (item == null) { continue; }

                    var thumbnail = (Texture)AssetPreview.GetMiniThumbnail(item);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(3f);

                            //------ icon ------

                            var toolTipText = GetToolTipText(item);

                            var originLabelWidth = EditorGUIUtility.labelWidth;

                            EditorGUIUtility.labelWidth = 0f;

                            var iconContent = new GUIContent(thumbnail, toolTipText);
                            
                            // アイコンが見切れるのでサイズを補正. 
                            GUILayout.Label(iconContent, labelStyle, GUILayout.Width(iconSize.x + 3.5f));

                            EditorGUIUtility.labelWidth = originLabelWidth;

                            //------ label ------

                            var labelText = GetLabelName(item);

                            var labelContent = new GUIContent(labelText, toolTipText);

                            GUILayout.Label(labelContent, labelStyle, GUILayout.Height(18f));
                        }

                        //------ mouse action ------

                        var rect = GUILayoutUtility.GetLastRect();

                        if (rect.Contains(e.mousePosition))
                        {
                            switch (e.button)
                            {
                                case 0:
                                    MouseRightButton(rect, e, item);
                                    break;

                                case 1:
                                    MouseLeftButton(rect, e, item);
                                    break;
                            }
                        }
                    }
                }

                EditorGUIUtility.SetIconSize(originIconSize);

                scrollPosition = scrollViewScope.scrollPosition;

                // ドロップエリア
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                    case EventType.DragPerform:

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

                        break;
                }
            }
        }

        private void MouseRightButton(Rect rect, Event e, Object item)
        {
            if (e.type == EventType.MouseDown)
            {
                switch (e.clickCount)
                {
                    case 1:
                        EditorGUIUtility.PingObject(item);
                        break;

                    case 2:
                        OpenAsset(item);
                        break;
                }
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

                        Pin(DragAndDrop.objectReferences, item, toAbove);
                    }

                    e.Use();
                }
            }

            if (e.type == EventType.MouseDrag)
            {
                DragAndDrop.PrepareStartDrag();
                DragAndDrop.objectReferences = new[] { item };
                DragAndDrop.StartDrag("Dragging");

                e.Use();
            }
        }

        private void MouseLeftButton(Rect rect, Event e, Object item)
        {
            if (e.type == EventType.MouseDown)
            {
                var menu = new GenericMenu();

                GenericMenu.MenuFunction onMenuRemoveCommand = () =>
                {
                    pinnedObject.Remove(item);
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
                if (pinnedObject.Contains(obj))
                {
                    pinnedObject.Remove(obj);
                }

                if (adjacentObject != null && pinnedObject.Any())
                {
                    var insertion = pinnedObject.IndexOf(adjacentObject) + (toAbove ? 0 : 1);

                    var index = Mathf.Clamp(insertion, 0, pinnedObject.Count);

                    pinnedObject.Insert(index, obj);
                }
                else
                {
                    pinnedObject.Add(obj);
                }
            }

            Save();

            Repaint();
        }

        private void OpenAsset(Object obj)
        {
            AssetDatabase.OpenAsset(obj);
            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        protected virtual void UpdatePinnedObject()
        {
            // NullになったObjectの削除.
            for (var i = pinnedObject.Count - 1; 0 <= i; --i)
            {
                if (pinnedObject[i] == null)
                {
                    pinnedObject.RemoveAt(i);
                }
            }
        }

        protected abstract void Save();

        protected abstract void Load();

        protected abstract string GetToolTipText(Object item);

        protected abstract string GetLabelName(Object item);

        protected abstract bool ValidatePinned(Object[] items);
    }
}
