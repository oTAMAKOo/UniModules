﻿﻿
using UnityEngine;
using UnityEditor;
using System.Collections;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.Atlas
{
    public static class EditorAtlasPrefs
    {
        public static AtlasTexture atlas
        {
            get { return ProjectPrefs.GetAsset<AtlasTexture>("CurrentAtlas", null); }
            set { ProjectPrefs.SetAsset("CurrentAtlas", value); }
        }

        public static string spriteSearchText
        {
            get { return ProjectPrefs.GetString("SpriteSearchText", null); }
            set { ProjectPrefs.SetString("SpriteSearchText", value); }
        }

        public static string selectedSprite
        {
            get { return ProjectPrefs.GetString("SelectedSprite", null); }
            set { ProjectPrefs.SetString("SelectedSprite", value); }
        }
    }

    [CustomEditor(typeof(AtlasTexture))]
    public class AtlasTextureInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private AtlasTexture atlas = null;

        public static AtlasTextureInspector instance = null;

        //----- property -----

        //----- method -----

        void OnEnable() { instance = this; }

        void OnDisable() { instance = null; }

        public override void OnInspectorGUI()
        {
            atlas = target as AtlasTexture;

            CustomInspector();
        }

        public static void SelectSprite(string spriteName)
        {
            if (EditorAtlasPrefs.atlas != null)
            {
                EditorAtlasPrefs.selectedSprite = spriteName;
                Selection.activeObject = EditorAtlasPrefs.atlas;
                RepaintSprites();
            }
        }

        public static void RepaintSprites()
        {
            if (AtlasPacker.instance != null)
            {
                AtlasPacker.instance.Repaint();
            }

            if (instance != null)
            {
                instance.Repaint();
            }
        }

        public void CustomInspector()
        {
            EditorLayoutTools.SetLabelWidth(80f);

            var sprite = (atlas != null) ? atlas.GetSpriteData(EditorAtlasPrefs.selectedSprite) : null;

            EditorGUILayout.Separator();

            if (atlas.Texture != null)
            {
                if (sprite == null && atlas.Sprites.Count > 0)
                {
                    string spriteName = EditorAtlasPrefs.selectedSprite;

                    if (!string.IsNullOrEmpty(spriteName))
                    {
                        sprite = atlas.GetSpriteData(spriteName);
                    }

                    if (sprite == null)
                    {
                        sprite = atlas.Sprites[0];
                    }
                }

                if (sprite != null)
                {
                    var tex = atlas.Texture as Texture2D;

                    if (tex != null)
                    {
                        EditorGUILayout.Separator();

                        DrawAdvancedSpriteField(atlas, sprite.name);

                        EditorGUILayout.Separator();
                        
                        EditorLayoutTools.DrawContentTitle("Sprite Size");

                        using (new ContentsScope())
                        {
                            EditorLayoutTools.IntRangeField(null, "Width", "Height", sprite.width, sprite.height, false);
                        }

                        EditorLayoutTools.DrawContentTitle("Sprite Border");

                        using (new ContentsScope())
                        {
                            GUI.changed = false;

                            var borderA = EditorLayoutTools.DelayedIntRangeField(null, "Left", "Right", sprite.borderLeft, sprite.borderRight);
                            var borderB = EditorLayoutTools.DelayedIntRangeField(null, "Bottom", "Top", sprite.borderBottom, sprite.borderTop);

                            if (GUI.changed)
                            {
                                UnityEditorUtility.RegisterUndo("Atlas Change", atlas);

                                sprite.borderLeft = borderA.x;
                                sprite.borderRight = borderA.y;
                                sprite.borderBottom = borderB.x;
                                sprite.borderTop = borderB.y;

                                atlas.CacheClear();
                            }
                        }

                        GUILayout.Space(2f);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Update"))
                            {
                                UnityEditorUtility.SaveAsset(atlas);
                            }
                        }
                    }
                }
            }
        }

        private static void DrawAdvancedSpriteField(AtlasTexture atlas, string spriteName)
        {
            if (atlas == null) { return; }

            if (atlas.Sprites.Count == 0)
            {
                EditorGUILayout.HelpBox("No sprites found", MessageType.Warning);
                return;
            }

            GUILayout.BeginHorizontal();
            {
                if (EditorLayoutTools.DrawPrefixButton("Sprite"))
                {
                    EditorAtlasPrefs.atlas = atlas;
                    EditorAtlasPrefs.selectedSprite = spriteName;
                    AtlasSpriteSelector.Show(SelectSprite);
                }

                EditorGUILayout.SelectableLabel(spriteName, new GUIStyle("TextArea"), GUILayout.Height(18f));
            }
            GUILayout.EndHorizontal();
        }
        
        public override bool HasPreviewGUI() { return true; }
        
        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            var sprite = (atlas != null) ? atlas.GetSpriteData(EditorAtlasPrefs.selectedSprite) : null;

            if (sprite == null) { return; }

            var tex = atlas.Texture as Texture2D;

            if (tex != null)
            {
                var isHeaderPreview = rect.width == 32 && rect.height == 32;

                DrawSprite(tex, rect, sprite, Color.white, !isHeaderPreview);
            }
        }

        private static void DrawSprite(Texture2D tex, Rect drawRect, SpriteData sprite, Color color, bool hasLabel)
        {
            if (!tex || sprite == null){ return; }

            EditorLayoutTools.DrawSprite(
                tex, 
                drawRect, 
                color, 
                null, 
                sprite.x, sprite.y, 
                sprite.width, sprite.height,
                sprite.borderLeft, sprite.borderBottom, sprite.borderRight, sprite.borderTop,
                hasLabel);
        }
    }
}
