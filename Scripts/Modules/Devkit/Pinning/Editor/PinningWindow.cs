
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

		private GUIStyle labelStyle = null;
		private GUIStyle iconStyle = null;

        private Vector2 scrollPosition = Vector2.zero;
        private Vector2 lastClickPosition = Vector2.zero;

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
			labelStyle = null;
			iconStyle = null;

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

                var requestSave = false;

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
                            GUILayout.Label(iconContent, iconStyle, GUILayout.Width(iconSize.x + 2.5f), GUILayout.Height(iconSize.y));

                            EditorGUIUtility.labelWidth = originLabelWidth;

                            //------ label ------

							using (new EditorGUILayout.VerticalScope())
							{
								GUILayout.Space(-0.5f);

	                            var labelText = GetLabelName(item.target);

	                            var labelContent = new GUIContent(labelText, toolTipText);

	                            GUILayout.Label(labelContent, labelStyle, GUILayout.Height(18f));
							}

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
                            var doubleClick = IsDoubleClick(e);

                            switch (e.button)
                            {
                                case 0:
                                    MouseLeftButton(rect, e, item, doubleClick);
                                    break;

                                case 1:
                                    MouseRightButton(rect, e, item, doubleClick);
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

        private bool IsDoubleClick(Event e)
        {
            var wasclick = e.type == EventType.MouseDown;
            var wasused = e.type == EventType.Used;

            if (wasclick && !wasused) 
            {
                if ((lastClickPosition - e.mousePosition).sqrMagnitude <= 5 * 5 && e.clickCount > 1)
                {
                    return true;
                }
                             
                lastClickPosition = e.mousePosition;
            }

            return false;
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
		}

        private void MouseLeftButton(Rect rect, Event e, PinnedItem item, bool doubleClick)
        {
            if (e.type == EventType.MouseDown)
            {
                OnMouseLeftDown(item.target, doubleClick);
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

        private void MouseRightButton(Rect rect, Event e, PinnedItem item, bool doubleClick)
        {
            if (e.type == EventType.MouseDown)
            {
                var menu = new GenericMenu();

                void OnMenuCommentCommand()
                {
                    commentInputTarget = item;
                }

                menu.AddItem(new GUIContent("Comment"), false, OnMenuCommentCommand);

                void OnMenuRemoveCommand()
                {
                    pinning.Remove(item);
                    Save();
                }

                menu.AddItem(new GUIContent("Remove"), false, OnMenuRemoveCommand);

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
                else
                {
                    item = new PinnedItem() { target = obj };
                }

                if (adjacentObject != null && pinning.Any())
                {
                    var insertion = pinning.FindIndex(x => x.target == adjacentObject) + (toAbove ? 0 : 1);

                    var index = Mathf.Clamp(insertion, 0, pinning.Count);

                    pinning.Insert(index, item);
                }
                else
                {
                    pinning.Add(item);
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

        protected abstract void OnMouseLeftDown(Object item, bool doubleClick);
    }
}
