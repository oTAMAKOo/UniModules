
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using UniRx;

namespace Modules.AtlasTexture
{
    [CustomEditor(typeof(AtlasTextureImage))]
    public class AtlasTextureImageInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private IDisposable onSelectSpriteDisposable = null;

        //----- property -----

        //----- method -----

        private void OnDisable()
        {
            if (onSelectSpriteDisposable != null)
            {
                onSelectSpriteDisposable.Dispose();
            }
        }

        public override void OnInspectorGUI()
        {
            var atlasTextureImage = target as AtlasTextureImage;

            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                var enableSelectSprite = atlasTextureImage.AtlasTexture != null && atlasTextureImage.AtlasTexture.SpriteData.Any();

                using (new DisableScope(!enableSelectSprite))
                {
                    if (EditorLayoutTools.DrawPrefixButton("Sprite"))
                    {
                        var spriteSelector = SpriteSelector.Open(atlasTextureImage.AtlasTexture, atlasTextureImage.SpriteName);

                        if (spriteSelector != null)
                        {
                            if (onSelectSpriteDisposable != null)
                            {
                                onSelectSpriteDisposable.Dispose();
                            }

                            onSelectSpriteDisposable = spriteSelector.OnSelectSpriteAsObservable()
                                .Subscribe(x => OnSelectSprite(atlasTextureImage, x));
                        }
                    }
                }

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(4f);

                    EditorGUI.BeginChangeCheck();

                    var atlasTexture = EditorGUILayout.ObjectField(atlasTextureImage.AtlasTexture, typeof(AtlasTexture), false) as AtlasTexture;

                    if (EditorGUI.EndChangeCheck())
                    {
                        UnityEditorUtility.RegisterUndo("AtlasTextureImage Undo", atlasTextureImage);
                        Reflection.SetPrivateField(atlasTextureImage, "atlasTexture", atlasTexture);
                    }
                }
            }
        
            GUILayout.Space(2f);

            if (!string.IsNullOrEmpty(atlasTextureImage.SpriteName))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField("SpriteName", GUILayout.Width(76f), GUILayout.Height(18f));

                    EditorGUILayout.SelectableLabel(atlasTextureImage.SpriteName, EditorStyles.textArea, GUILayout.Height(18f));

                    GUILayout.Space(17f);
                }
            }
        }

        private void OnSelectSprite(AtlasTextureImage atlasTextureImage, string spriteName)
        {
            if (atlasTextureImage.AtlasTexture == null) { return; }
            
            UnityEditorUtility.RegisterUndo("AtlasTextureImage Undo", atlasTextureImage);

            atlasTextureImage.SpriteName = spriteName;
        }
    }
}
