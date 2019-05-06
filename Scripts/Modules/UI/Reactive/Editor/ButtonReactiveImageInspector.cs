
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.AtlasTexture;
using UniRx;

namespace Modules.UI.Reactive
{
    [CustomEditor(typeof(ButtonReactiveImage))]
    public class ButtonReactiveImageInspector : UnityEditor.Editor
    {
        //----- params -----

        //----- field -----

        private ButtonReactiveImage instance = null;

        private IDisposable spriteSelectDisposable = null;

        public static ButtonReactiveImageInspector inspector = null;

        //----- property -----

        //----- method -----

        void OnEnable() { inspector = this; }

        void OnDisable()
        {
            inspector = null;

            if (spriteSelectDisposable != null)
            {
                spriteSelectDisposable.Dispose();
                spriteSelectDisposable = null;
            }
        }

        public static void SelectEnableSprite(string spriteName)
        {
            if (inspector == null) { return; }

            if (inspector.instance.AtlasTexture != null)
            {
                UnityEditorUtility.RegisterUndo("ButtonReactiveImageInspector Undo", inspector.instance);

                inspector.instance.EnableSpriteName = spriteName;
            }
        }

        public static void SelectDisableSprite(string spriteName)
        {
            if (inspector == null) { return; }

            if (inspector.instance.AtlasTexture != null)
            {
                UnityEditorUtility.RegisterUndo("ButtonReactiveImageInspector Undo", inspector.instance);

                inspector.instance.DisableSpriteName = spriteName;
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            instance = target as ButtonReactiveImage;

            if (instance.AtlasTexture != null)
            {
                var backgroundColor = new Color(0.3f, 0.3f, 0.5f);
                var labelColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);

                if (instance.AtlasTexture.SpriteData.Any())
                {
                    EditorGUILayout.Separator();

                    EditorLayoutTools.DrawLabelWithBackground("EnableSprite", backgroundColor, labelColor);

                    using (new GUILayout.HorizontalScope())
                    {
                        var enableSpriteName = Reflection.GetPrivateField<ButtonReactiveImage, string>(instance, "enableSpriteName");

                        GUILayout.Space(5f);

                        if (EditorLayoutTools.DrawPrefixButton("Sprite"))
                        {
                            if (spriteSelectDisposable != null)
                            {
                                spriteSelectDisposable.Dispose();
                                spriteSelectDisposable = null;
                            }

                            var spriteSelector = SpriteSelector.Open(instance.AtlasTexture, enableSpriteName);

                            spriteSelectDisposable = spriteSelector.OnSelectSpriteAsObservable().Subscribe(x => SelectEnableSprite(x));
                        }

                        if (!string.IsNullOrEmpty(enableSpriteName))
                        {
                            EditorGUILayout.SelectableLabel(enableSpriteName, EditorStyles.textArea, GUILayout.Height(18f));
                        }
                    }

                    EditorGUILayout.Separator();

                    EditorLayoutTools.DrawLabelWithBackground("DisableSprite", backgroundColor, labelColor);

                    using (new GUILayout.HorizontalScope())
                    {
                        var disableSpriteName = Reflection.GetPrivateField<ButtonReactiveImage, string>(instance, "disableSpriteName");

                        GUILayout.Space(5f);

                        if (EditorLayoutTools.DrawPrefixButton("Sprite"))
                        {
                            if (spriteSelectDisposable != null)
                            {
                                spriteSelectDisposable.Dispose();
                                spriteSelectDisposable = null;
                            }

                            var spriteSelector = SpriteSelector.Open(instance.AtlasTexture, disableSpriteName);

                            spriteSelectDisposable = spriteSelector.OnSelectSpriteAsObservable().Subscribe(x => SelectDisableSprite(x));
                        }

                        if (!string.IsNullOrEmpty(disableSpriteName))
                        {
                            EditorGUILayout.SelectableLabel(disableSpriteName, EditorStyles.textArea, GUILayout.Height(18f));
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("No sprites found", MessageType.Warning);
                }
            }
        }
    }
}
