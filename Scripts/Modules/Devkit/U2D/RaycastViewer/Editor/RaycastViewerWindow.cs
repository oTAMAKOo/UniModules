
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.U2D
{
    public sealed class RaycastViewerWindow : SingletonEditorWindow<RaycastViewerWindow>
    {
        //----- params -----

        //----- field -----

        private RaycastResult[] raycastResults = null;
        private Vector2 scrollPosition = Vector2.zero;

        [NonSerialized]
        private bool initialized = false;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            Instance.Initialize();

            Instance.Show();
        }

        private void Initialize()
        {
            if (initialized) { return; }

            titleContent = new GUIContent("RaycastViewer");

            raycastResults = new RaycastResult[0];

            Observable.EveryUpdate().Where(_ => Input.GetMouseButtonDown(0))
                .Subscribe(_ => UpdateRaycastObjects())
                .AddTo(Disposable);

            initialized = true;
        }

        void OnGUI()
        {
            var e = Event.current;

            if (!initialized)
            {
                Initialize();
            }

            if (Application.isPlaying)
            {
                if (raycastResults.Any())
                {
                    GUILayout.Space(3f);

                    
                    GUILayout.Space(2f);

                    var labelStyle = new GUIStyle(GUI.skin.label);

                    labelStyle.alignment = TextAnchor.MiddleLeft;

                    var originIconSize = EditorGUIUtility.GetIconSize();

                    var iconSize = new Vector2(16f, 16f);

                    EditorGUIUtility.SetIconSize(iconSize);

                    using (var scrollView = new EditorGUILayout.ScrollViewScope(scrollPosition))
                    {
                        foreach (var raycastResult in raycastResults)
                        {
                            var gameObject = raycastResult.gameObject;

                            var thumbnail = (Texture)AssetPreview.GetMiniThumbnail(gameObject);

                            using (new EditorGUILayout.HorizontalScope())
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    GUILayout.Space(3f);

                                    //------ icon ------

                                    var hierarchyPath = UnityUtility.GetChildHierarchyPath(null, gameObject);

                                    var toolTipText = PathUtility.Combine(hierarchyPath, gameObject.transform.name);

                                    var originLabelWidth = EditorGUIUtility.labelWidth;

                                    EditorGUIUtility.labelWidth = 0f;

                                    var iconContent = new GUIContent(thumbnail, toolTipText);

                                    // アイコンが見切れるのでサイズを補正. 
                                    GUILayout.Label(iconContent, labelStyle, GUILayout.Width(iconSize.x + 3.5f));

                                    EditorGUIUtility.labelWidth = originLabelWidth;

                                    //------ label ------

                                    var labelText = gameObject.name;

                                    var labelContent = new GUIContent(labelText, toolTipText);

                                    GUILayout.Label(labelContent, labelStyle, GUILayout.Height(18f));

                                    GUILayout.FlexibleSpace();

                                    //------ depth ------

                                    var depthText = raycastResult.depth.ToString();

                                    var depthContent = new GUIContent(depthText);

                                    GUILayout.Label(depthContent, labelStyle, GUILayout.Height(18f));

                                    GUILayout.Space(5f);
                                }

                                //------ mouse action ------

                                var rect = GUILayoutUtility.GetLastRect();

                                if (rect.Contains(e.mousePosition))
                                {
                                    switch (e.button)
                                    {
                                        case 0:
                                            Selection.activeGameObject = gameObject;
                                            break;
                                    }
                                }
                            }
                        }

                        EditorGUIUtility.SetIconSize(originIconSize);

                        scrollPosition = scrollView.scrollPosition;
                    }

                    GUILayout.Space(3f);
                }
                else
                {
                    EditorGUILayout.HelpBox("Click GameView to search for objects.", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Only works while playing.", MessageType.Warning);
            }
        }

        private void UpdateRaycastObjects()
        {
            if (!IsFocusedGameViewWindow()) { return; }

            raycastResults = GetRaycastObjects(Input.mousePosition);

            Repaint();
        }

        private static bool IsFocusedGameViewWindow()
        {
            if (EditorWindow.focusedWindow == null) { return false; }

            return EditorWindow.focusedWindow.titleContent.text == "Game";
        }

        private static RaycastResult[] GetRaycastObjects(Vector3 position)
        {
            var eventSystem = EventSystem.current;

            if (eventSystem == null) { return new RaycastResult[0]; }

            var pointer = new PointerEventData(eventSystem);

            pointer.position = position;

            var raycastResults = new List<RaycastResult>();

            eventSystem.RaycastAll(pointer, raycastResults);
            
            return raycastResults.OrderByDescending(x => x.depth).ToArray();
        }
    }
}
