﻿﻿
using UnityEngine;
using UnityEditor;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.SpriteSheet
{
    [CustomEditor(typeof(SpriteSheetImage))]
    public class SpriteSheetImageInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private SpriteSheetImage spriteSheetImage = null;

        public static SpriteSheetImageInspector instance = null;

        //----- property -----

        //----- method -----

        void OnEnable() { instance = this; }

        void OnDisable() { instance = null; }

        public override void OnInspectorGUI()
        {
            spriteSheetImage = target as SpriteSheetImage;

            CustomInspector();
        }

        public static void SelectSprite(string spriteName)
        {
            if(instance == null) { return; }

            if (instance.spriteSheetImage.SpriteSheet != null)
            {
                UnityEditorUtility.RegisterUndo("SpriteSheetImage Undo", instance.spriteSheetImage);
                instance.spriteSheetImage.SpriteName = spriteName;
            }
        }

        private void CustomInspector()
        {
            EditorGUILayout.Separator();

            EditorGUI.BeginChangeCheck();

            var spriteSheet = (SpriteSheet)EditorGUILayout.ObjectField("SpriteSheet", spriteSheetImage.SpriteSheet, typeof(SpriteSheet), false);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("SpriteSheetImage Undo", instance.spriteSheetImage);
                Reflection.SetPrivateField(spriteSheetImage, "spriteSheet", spriteSheet);
            }

            if (spriteSheetImage.SpriteSheet != null)
            {
                EditorGUILayout.Separator();

                if (spriteSheetImage.SpriteSheet.Sprites.Any())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (EditorLayoutTools.DrawPrefixButton("Sprite"))
                        {
                            EditorSpriteSheetPrefs.spriteSheet = spriteSheetImage.SpriteSheet;
                            EditorSpriteSheetPrefs.selectedSprite = spriteSheetImage.SpriteName;
                            SpriteSelector.Show(SelectSprite);
                        }

                        if (!string.IsNullOrEmpty(spriteSheetImage.SpriteName))
                        {
                            EditorGUILayout.SelectableLabel(spriteSheetImage.SpriteName, EditorStyles.textArea, GUILayout.Height(18f));
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No sprites found", MessageType.Warning);
                }
            }

            EditorGUILayout.Separator();
        }
    }
}
