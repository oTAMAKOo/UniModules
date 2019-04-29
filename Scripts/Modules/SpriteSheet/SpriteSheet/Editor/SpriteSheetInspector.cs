﻿﻿
using UnityEngine;
using UnityEditor;
using System.Collections;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;

namespace Modules.SpriteSheet
{
    public static class EditorSpriteSheetPrefs
    {
        public static SpriteSheet spriteSheet
        {
            get { return ProjectPrefs.GetAsset<SpriteSheet>("EditorSpriteSheetPrefs-spriteSheet", null); }
            set { ProjectPrefs.SetAsset("EditorSpriteSheetPrefs-spriteSheet", value); }
        }

        public static string spriteSearchText
        {
            get { return ProjectPrefs.GetString("EditorSpriteSheetPrefs-spriteSearchText", null); }
            set { ProjectPrefs.SetString("EditorSpriteSheetPrefs-spriteSearchText", value); }
        }

        public static string selectedSprite
        {
            get { return ProjectPrefs.GetString("EditorSpriteSheetPrefs-selectedSprite", null); }
            set { ProjectPrefs.SetString("EditorSpriteSheetPrefs-selectedSprite", value); }
        }
    }

    [CustomEditor(typeof(SpriteSheet))]
    public class SpriteSheetInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private SpriteSheet spriteSheet = null;

        public static SpriteSheetInspector instance = null;

        //----- property -----

        //----- method -----

        void OnEnable() { instance = this; }

        void OnDisable() { instance = null; }

        public override void OnInspectorGUI()
        {
            spriteSheet = target as SpriteSheet;

            CustomInspector();
        }

        public static void SelectSprite(string spriteName)
        {
            if (EditorSpriteSheetPrefs.spriteSheet != null)
            {
                EditorSpriteSheetPrefs.selectedSprite = spriteName;
                Selection.activeObject = EditorSpriteSheetPrefs.spriteSheet;
                RepaintSprites();
            }
        }

        public static void RepaintSprites()
        {
            if (SpriteSheetMaker.instance != null)
            {
                SpriteSheetMaker.instance.Repaint();
            }

            if (instance != null)
            {
                instance.Repaint();
            }
        }

        public void CustomInspector()
        {
            EditorLayoutTools.SetLabelWidth(80f);

            var sprite = (spriteSheet != null) ? spriteSheet.GetSpriteData(EditorSpriteSheetPrefs.selectedSprite) : null;

            EditorGUILayout.Separator();

            if (spriteSheet.Texture != null)
            {
                if (sprite == null && spriteSheet.Sprites.Count > 0)
                {
                    string spriteName = EditorSpriteSheetPrefs.selectedSprite;

                    if (!string.IsNullOrEmpty(spriteName))
                    {
                        sprite = spriteSheet.GetSpriteData(spriteName);
                    }

                    if (sprite == null)
                    {
                        sprite = spriteSheet.Sprites[0];
                    }
                }

                if (sprite != null)
                {
                    var tex = spriteSheet.Texture as Texture2D;

                    if (tex != null)
                    {
                        EditorGUILayout.Separator();

                        DrawAdvancedSpriteField(spriteSheet, sprite.name);

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
                                UnityEditorUtility.RegisterUndo("SpriteSheet Change", spriteSheet);

                                sprite.borderLeft = borderA.x;
                                sprite.borderRight = borderA.y;
                                sprite.borderBottom = borderB.x;
                                sprite.borderTop = borderB.y;

                                spriteSheet.CacheClear();
                            }
                        }

                        GUILayout.Space(2f);

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button("Update"))
                            {
                                UnityEditorUtility.SaveAsset(spriteSheet);
                            }
                        }
                    }
                }
            }
        }

        private static void DrawAdvancedSpriteField(SpriteSheet spriteSheet, string spriteName)
        {
            if (spriteSheet == null) { return; }

            if (spriteSheet.Sprites.Count == 0)
            {
                EditorGUILayout.HelpBox("No sprites found", MessageType.Warning);
                return;
            }

            GUILayout.BeginHorizontal();
            {
                if (EditorLayoutTools.DrawPrefixButton("Sprite"))
                {
                    EditorSpriteSheetPrefs.spriteSheet = spriteSheet;
                    EditorSpriteSheetPrefs.selectedSprite = spriteName;
                    SpriteSelector.Show(SelectSprite);
                }

                EditorGUILayout.SelectableLabel(spriteName, new GUIStyle("TextArea"), GUILayout.Height(18f));
            }
            GUILayout.EndHorizontal();
        }
        
        public override bool HasPreviewGUI() { return true; }
        
        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            var sprite = (spriteSheet != null) ? spriteSheet.GetSpriteData(EditorSpriteSheetPrefs.selectedSprite) : null;

            if (sprite == null) { return; }

            var tex = spriteSheet.Texture as Texture2D;

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
