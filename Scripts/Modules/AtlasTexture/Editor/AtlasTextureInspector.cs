
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Prefs;
using UniRx;
using UnityEngine.U2D;

namespace Modules.AtlasTexture
{
    [CustomEditor(typeof(AtlasTexture))]
    public class AtlasTextureInspector : UnityEditor.Editor
    {
        //----- params -----

        public static class Prefs
        {
            public static string selectedSprite
            {
                get { return ProjectPrefs.GetString("AtlasTextureInspectorPrefs-selectedSprite", null); }
                set { ProjectPrefs.SetString("AtlasTextureInspectorPrefs-selectedSprite", value); }
            }
        }

        //----- field -----

        private AtlasTexture atlasTexture = null;

        private Sprite currentSprite = null;
        private Rect spriteRect = Rect.zero;
        private Vector4 spriteBorder = Vector4.zero;
        private bool changeBorder = false;

        private IDisposable onSelectSpriteDisposable = null;

        private static AtlasTextureInspector instance = null;

        //----- property -----

        //----- method -----

        void OnEnable()
        {
            instance = this;

            atlasTexture = target as AtlasTexture;

            currentSprite = null;

            SetCurrentSprite(Prefs.selectedSprite);
        }

        void OnDisable()
        {
            instance = null;

            if (onSelectSpriteDisposable != null)
            {
                onSelectSpriteDisposable.Dispose();
            }
        }

        public override void OnInspectorGUI()
        {
            atlasTexture = target as AtlasTexture;

            var spriteAtlas = Reflection.GetPrivateField<AtlasTexture, SpriteAtlas>(atlasTexture, "spriteAtlas");

            var originLabelWidth = EditorLayoutTools.SetLabelWidth(80f);

            EditorGUILayout.ObjectField(spriteAtlas, typeof(SpriteAtlas), false);

            GUILayout.Space(2f);

            var currentSpriteName = currentSprite != null ? currentSprite.name : string.Empty;

            using (new EditorGUILayout.HorizontalScope())
            {
                var enableSelectSprite = atlasTexture != null && atlasTexture.SpriteData.Any();

                using (new DisableScope(!enableSelectSprite))
                {
                    if (EditorLayoutTools.DrawPrefixButton("Sprite"))
                    {
                        var spriteSelector = SpriteSelector.Open(atlasTexture, currentSpriteName);

                        if (spriteSelector != null)
                        {
                            if (onSelectSpriteDisposable != null)
                            {
                                onSelectSpriteDisposable.Dispose();
                            }

                            onSelectSpriteDisposable = spriteSelector.OnSelectSpriteAsObservable()
                                .Subscribe(x => SetCurrentSprite(x));
                        }
                    }
                }

                EditorGUILayout.SelectableLabel(currentSpriteName, EditorStyles.textArea, GUILayout.Height(18f));

                if (currentSprite != null)
                {
                    using (new EditorGUILayout.VerticalScope(GUILayout.Width(50f)))
                    {
                        GUILayout.Space(1f);

                        if (GUILayout.Button("Edit", GUILayout.Width(50f), GUILayout.Height(18f)))
                        {
                            AtlasTextureUtility.OpenSpriteEditorWindow(currentSprite);
                        }
                    }
                }
            }

            GUILayout.Space(2f);

            if (currentSprite != null)
            {
                EditorLayoutTools.DrawContentTitle("Sprite Size");

                using (new ContentsScope())
                {
                    EditorLayoutTools.IntRangeField(null, "Width", "Height", (int)spriteRect.width, (int)spriteRect.height, false);
                }

                EditorLayoutTools.DrawContentTitle("Sprite Border");

                using (new ContentsScope())
                {
                    GUI.changed = false;

                    var borderA = EditorLayoutTools.DelayedIntRangeField(null, "Left", "Top", (int)spriteBorder.x, (int)spriteBorder.w);
                    var borderB = EditorLayoutTools.DelayedIntRangeField(null, "Right", "Bottom", (int)spriteBorder.z, (int)spriteBorder.y);

                    if (GUI.changed)
                    {
                        UnityEditorUtility.RegisterUndo("AtlasTextureInspector Undo", atlasTexture);

                        spriteBorder.x = borderA.x;
                        spriteBorder.w = borderA.y;
                        spriteBorder.z = borderB.x;
                        spriteBorder.y = borderB.y;

                        changeBorder = true;                        
                    }
                }

                GUILayout.Space(2f);                
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Pack & Update", GUILayout.Width(150f)))
                {
                    AtlasTextureUpdater.SetAtlasSpriteData(atlasTexture);
                }

                GUILayout.FlexibleSpace();

                if (currentSprite != null)
                {
                    using (new DisableScope(!changeBorder))
                    {
                        if (GUILayout.Button("Apply", GUILayout.Width(70f)))
                        {
                            ApplySpriteEdit();
                        }
                    }
                }
            }

            EditorLayoutTools.SetLabelWidth(originLabelWidth);
        }

        private void ApplySpriteEdit()
        {
            if (currentSprite == null) { return; }

            var texture = currentSprite.texture;

            var textureAssetPath = AssetDatabase.GetAssetPath(texture);

            var textureImporter = AssetImporter.GetAtPath(textureAssetPath) as TextureImporter;

            if (textureImporter == null) { return; }

            if (textureImporter.spritesheet.Any())
            {
                var index = textureImporter.spritesheet.IndexOf(x => x.name == currentSprite.name);
                
                textureImporter.spritesheet[index].border = spriteBorder;
            }
            else
            {
                textureImporter.spriteBorder = spriteBorder;
            }

            textureImporter.SaveAndReimport();

            AssetDatabase.Refresh();

            currentSprite = null;

            SetCurrentSprite(Prefs.selectedSprite);

            Repaint();

            atlasTexture.CacheClear();

            AtlasTextureUpdater.UpdateAtlasTextureImage(atlasTexture);

            changeBorder = false;
        }

        private void SetCurrentSprite(string spriteName)
        {
            if (atlasTexture == null) { return; }

            var sprite = atlasTexture.GetSprite(spriteName);

            LoadSpriteInfo(sprite);

            Prefs.selectedSprite = spriteName;
        }

        public override bool HasPreviewGUI() { return true; }

        public override void OnPreviewGUI(Rect rect, GUIStyle background)
        {
            if (currentSprite == null) { return; }

            var isHeaderPreview = rect.width == 32 && rect.height == 32;
            
            EditorLayoutTools.DrawSprite(currentSprite.texture, rect,
                                         Color.white, null,
                                         spriteRect.x, spriteRect.y,
                                         spriteRect.width, spriteRect.height,
                                         spriteBorder.x, spriteBorder.y, spriteBorder.z, spriteBorder.w,
                                         isHeaderPreview);
        }

        private void LoadSpriteInfo(Sprite sprite)
        {
            if (sprite == null) { return; }

            if (currentSprite != null && currentSprite == sprite) { return; }

            var texture = sprite.texture;

            var textureAssetPath = AssetDatabase.GetAssetPath(texture);

            var textureImporter = AssetImporter.GetAtPath(textureAssetPath) as TextureImporter;

            if (textureImporter.spritesheet.Any())
            {
                var spriteMetaData = textureImporter.spritesheet.FirstOrDefault(x => x.name == sprite.name);

                spriteRect = spriteMetaData.rect;
                spriteBorder = spriteMetaData.border;

            }
            else
            {
                spriteRect = new Rect(0f, 0f, texture.width, texture.height);
                spriteBorder = textureImporter.spriteBorder;
            }

            currentSprite = sprite;
        }

        public static void RepaintSprites()
        {
            if (instance != null)
            {
                instance.Repaint();
            }
        }
    }
}
