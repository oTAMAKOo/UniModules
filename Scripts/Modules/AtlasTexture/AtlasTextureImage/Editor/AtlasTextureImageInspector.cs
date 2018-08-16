﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.Atlas
{
    [CustomEditor(typeof(AtlasTextureImage))]
    public class AtlasTextureImageInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private AtlasTextureImage atlasTextureImage = null;

        public static AtlasTextureImageInspector instance = null;

        //----- property -----

        //----- method -----

        void OnEnable() { instance = this; }

        void OnDisable() { instance = null; }

        public override void OnInspectorGUI()
        {
            atlasTextureImage = target as AtlasTextureImage;

            CustomInspector();
        }

        public static void SelectSprite(string spriteName)
        {
            if(instance == null) { return; }

            if (instance.atlasTextureImage.Atlas != null)
            {
                UnityEditorUtility.RegisterUndo("AtlasTextureImage Undo", instance.atlasTextureImage);
                instance.atlasTextureImage.SpriteName = spriteName;
            }
        }

        private void CustomInspector()
        {
            EditorGUILayout.Separator();

            EditorGUI.BeginChangeCheck();

            var atlasTexture = (AtlasTexture)EditorGUILayout.ObjectField("AtlasTexture", atlasTextureImage.Atlas, typeof(AtlasTexture), false);

            if (EditorGUI.EndChangeCheck())
            {
                UnityEditorUtility.RegisterUndo("AtlasTextureImage Undo", instance.atlasTextureImage);
                Reflection.SetPrivateField(atlasTextureImage, "atlas", atlasTexture);
            }

            if (atlasTextureImage.Atlas != null)
            {
                EditorGUILayout.Separator();

                if (atlasTextureImage.Atlas.Sprites.Any())
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        if (EditorLayoutTools.DrawPrefixButton("Sprite"))
                        {
                            EditorAtlasPrefs.atlas = atlasTextureImage.Atlas;
                            EditorAtlasPrefs.selectedSprite = atlasTextureImage.SpriteName;
                            AtlasSpriteSelector.Show(SelectSprite);
                        }

                        if (!string.IsNullOrEmpty(atlasTextureImage.SpriteName))
                        {
                            EditorGUILayout.SelectableLabel(atlasTextureImage.SpriteName, EditorStyles.textArea, GUILayout.Height(18f));
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