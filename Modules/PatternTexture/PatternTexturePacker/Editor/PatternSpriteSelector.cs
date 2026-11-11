
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.PatternTexture
{
    public sealed class PatternSpriteSelector : ScriptableWizard
    {
        //----- params -----

        //----- field -----

        private PatternTexture patternTexture = null;
        private Texture2D[] sourceTextures = null;
        private float preViewSize = 0;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchText = string.Empty;
        private string selectionTextureName = null;
        private Action<string> onSelectAction = null;
        private Action onCloseAction = null;

        public static PatternSpriteSelector instance = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            instance = this;
        }

        void OnDisable()
        {
            if(onCloseAction != null)
            {
                onCloseAction();
            }

            instance = null;
        }

        void OnGUI()
        {
            EditorLayoutTools.SetLabelWidth(80f);

            if (patternTexture == null)
            {
                EditorGUILayout.HelpBox("No PatternTexture selected.", MessageType.Info);
                return;
            }
           
            GUILayout.Space(15f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10f);

                EditorGUILayout.ObjectField(instance.patternTexture, typeof(PatternTexture), false, GUILayout.Width(400f));

                GUILayout.FlexibleSpace();

                preViewSize = EditorGUILayout.Slider(preViewSize, 100f, 500f, GUILayout.Width(150f));

                GUILayout.Space(25f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    var before = searchText;
                    var after = EditorGUILayout.TextField(string.Empty, before, "SearchTextField", GUILayout.Width(200f));

                    if (before != after)
                    {
                        searchText = after;
                    }

                    if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                    {
                        searchText = string.Empty;
                        GUIUtility.keyboardControl = 0;
                    }
                }
            }

            EditorGUILayout.Separator();

            if (sourceTextures.IsEmpty())
            {
                EditorGUILayout.HelpBox("The atlas doesn't have a texture to work with.", MessageType.Info);
                return;
            }

            var size = preViewSize;
            var padded = size + 5f;
            var columns = Mathf.FloorToInt(Screen.width / padded);
            var offset = 0;
            var rect = new Rect(10f, 0, size, size);

            if (columns < 1) columns = 1;

            GUILayout.Space(10f);

            using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
            {
                int rows = 1;

                while (offset < sourceTextures.Length)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        int col = 0;
                        rect.x = 10f;

                        for (; offset < sourceTextures.Length; ++offset)
                        {
                            var texture = sourceTextures[offset];

                            if (texture == null) continue;

                            if (GUI.Button(rect, ""))
                            {
                                if (Event.current.button == 0)
                                {
                                    var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                    if (texture.name.IsMatch(keywords))
                                    {
                                        if (onSelectAction != null)
                                        {
                                            onSelectAction(texture.name);
                                            Close();
                                        }
                                    }
                                }
                            }

                            if (Event.current.type == EventType.Repaint)
                            {
                                EditorLayoutTools.DrawTiledTexture(rect, EditorLayoutTools.backdropTexture);

                                var uv = new Rect(0f, 0f, 1f, 1f);

                                var scaleX = rect.width / uv.width;
                                var scaleY = rect.height / uv.height;

                                var aspect = (scaleY / scaleX) / ((float)texture.height / texture.width);
                                var clipRect = rect;

                                if (aspect != 1f)
                                {
                                    if (aspect < 1f)
                                    {
                                        var padding = size * (1f - aspect) * 0.5f;
                                        clipRect.xMin += padding;
                                        clipRect.xMax -= padding;
                                    }
                                    else
                                    {
                                        var padding = size * (1f - 1f / aspect) * 0.5f;
                                        clipRect.yMin += padding;
                                        clipRect.yMax -= padding;
                                    }
                                }

                                GUI.DrawTexture(clipRect, texture);

                                if (selectionTextureName == texture.name)
                                {
                                    EditorLayoutTools.Outline(rect, new Color(0.4f, 1f, 0f, 1f));
                                }
                            }

                            GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
                            GUI.contentColor = new Color(1f, 1f, 1f, 0.7f);
                            GUI.Label(new Rect(rect.x, rect.y + rect.height, rect.width, 32f), texture.name, "ProgressBarBack");
                            GUI.contentColor = Color.white;
                            GUI.backgroundColor = Color.white;

                            if (++col >= columns)
                            {
                                ++offset;
                                break;
                            }
                            rect.x += padded;
                        }
                    }

                    GUILayout.Space(padded);

                    rect.y += padded + 26;
                    ++rows;
                }

                GUILayout.Space(rows * 26);

                scrollPosition = scrollViewScope.scrollPosition;
            }
        }

        public static void Show(PatternTexture patternTexture, string selection, Action<string> onSelectAction, Action onCloseAction)
        {
            if (instance != null)
            {
                instance.Close();
                instance = null;
            }

            var comp = DisplayWizard<PatternSpriteSelector>("Select Sprite");

            comp.onSelectAction = onSelectAction;
            comp.onCloseAction = onCloseAction;
            
            comp.patternTexture = patternTexture;
            comp.selectionTextureName = selection;
            comp.preViewSize = 200f;

            comp.sourceTextures = patternTexture.GetAllPatternData()
                    .Select(x =>
                        {
                            var path = AssetDatabase.GUIDToAssetPath(x.Guid);
                            return AssetDatabase.LoadMainAssetAtPath(path) as Texture2D;
                        })
                    .Where(x => x != null)
                    .ToArray();            
        }
    }
}
