﻿
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Atlas
{
    public class AtlasSpriteSelector : ScriptableWizard
    {
        public static AtlasSpriteSelector instance;

        public delegate void Callback(string sprite);

        private Vector2 pos = Vector2.zero;
        private Callback callback;

        void OnEnable()
        {
            instance = this;
        }

        void OnDisable()
        {
            instance = null;
        }

        void OnGUI()
        {
            EditorLayoutTools.SetLabelWidth(80f);

            if (EditorAtlasPrefs.atlas == null)
            {
                EditorGUILayout.HelpBox("No Atlas selected.", MessageType.Info);
            }
            else
            {
                var atlas = EditorAtlasPrefs.atlas;

                GUILayout.Space(15f);

                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(10f);

                    var txtStyle = new GUIStyle();

                    txtStyle.fontSize = 12;
                    txtStyle.normal.textColor = Color.yellow;

                    GUILayout.Label("Atlas : " + atlas.name, txtStyle);

                    GUILayout.FlexibleSpace();

                    GUILayout.BeginHorizontal();
                    {
                        string before = EditorAtlasPrefs.spriteSearchText;
                        string after = EditorGUILayout.TextField(string.Empty, before, "SearchTextField", GUILayout.Width(200f));
                        if (before != after) EditorAtlasPrefs.spriteSearchText = after;

                        if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                        {
                            EditorAtlasPrefs.spriteSearchText = string.Empty;
                            GUIUtility.keyboardControl = 0;
                        }
                    }
                    GUILayout.EndHorizontal();

                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Separator();

                Texture2D tex = atlas.Texture as Texture2D;

                if (tex == null)
                {
                    EditorGUILayout.HelpBox("The atlas doesn't have a texture to work with.", MessageType.Info);
                    return;
                }

                var sprites = atlas.GetListOfSprites(EditorAtlasPrefs.spriteSearchText);

                var size = 80f;
                var padded = size + 10f;
                var columns = Mathf.FloorToInt(Screen.width / padded);
                var offset = 0;
                var rect = new Rect(10f, 0, size, size);

                if (columns < 1) columns = 1;

                GUILayout.Space(10f);

                pos = GUILayout.BeginScrollView(pos);

                int rows = 1;

                while (offset < sprites.Length)
                {
                    GUILayout.BeginHorizontal();
                    {
                        int col = 0;
                        rect.x = 10f;

                        for (; offset < sprites.Length; ++offset)
                        {
                            var sprite = atlas.GetSpriteData(sprites[offset]);

                            if (sprite == null) continue;

                            if (GUI.Button(rect, ""))
                            {
                                if (Event.current.button == 0)
                                {
                                    if (EditorAtlasPrefs.spriteSearchText != sprite.name)
                                    {
                                        EditorAtlasPrefs.selectedSprite = sprite.name;
                                        AtlasTextureInspector.RepaintSprites();

                                        if (callback != null)
                                        {
                                            callback(sprite.name);
                                            Close();
                                        }
                                    }
                                }
                            }

                            if (Event.current.type == EventType.Repaint)
                            {
                                EditorLayoutTools.DrawTiledTexture(rect, EditorLayoutTools.backdropTexture);

                                var uv = new Rect(sprite.x, sprite.y, sprite.width, sprite.height);
                                uv = TextureUtility.ConvertToTexCoords(uv, tex.width, tex.height);

                                var scaleX = rect.width / uv.width;
                                var scaleY = rect.height / uv.height;

                                var aspect = (scaleY / scaleX) / ((float)tex.height / tex.width);
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

                                GUI.DrawTextureWithTexCoords(clipRect, tex, uv);
                                
                                if (EditorAtlasPrefs.selectedSprite == sprite.name)
                                {
                                    EditorLayoutTools.DrawOutline(rect, new Color(0.4f, 1f, 0f, 1f));
                                }
                            }

                            GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
                            GUI.contentColor = new Color(1f, 1f, 1f, 0.7f);
                            GUI.Label(new Rect(rect.x, rect.y + rect.height, rect.width, 32f), sprite.name, "ProgressBarBack");
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
                    GUILayout.EndHorizontal();
                    GUILayout.Space(padded);
                    rect.y += padded + 26;
                    ++rows;
                }

                GUILayout.Space(rows * 26);

                GUILayout.EndScrollView();
            }
        }

        public static void ShowSelected()
        {
            if (EditorAtlasPrefs.atlas != null)
            {
                Show(delegate (string sel) { AtlasTextureInspector.SelectSprite(sel); });
            }
        }

        public static void Show(Callback callback)
        {
            if (instance != null)
            {
                instance.Close();
                instance = null;
            }

            var comp = ScriptableWizard.DisplayWizard<AtlasSpriteSelector>("Select Sprite");

            comp.callback = callback;
        }
    }
}