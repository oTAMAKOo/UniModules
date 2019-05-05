
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.AtlasTexture
{
    public class SpriteSelector : ScriptableWizard
    {
        //----- params -----

        //----- field -----

        private AtlasTexture atlasTexture = null;
        private Vector2 scrollPosition= Vector2.zero;
        private string[] spriteNames = null;
        private string selectionSpriteName = null;
        private string spriteSearchText = null;

        private Subject<string> onSelectSprite = null;

        private static SpriteSelector instance = null;

        //----- property -----

        //----- method -----

        public static SpriteSelector Open(AtlasTexture atlasTexture, string selectionSpriteName)
        {
            if (instance != null)
            {
                instance.Close();
            }

            instance = DisplayWizard<SpriteSelector>("Select Sprite");

            instance.atlasTexture = atlasTexture;
            instance.selectionSpriteName = selectionSpriteName;

            instance.spriteNames = instance.GetListOfSprites(null);

            return instance;
        }

        void OnGUI()
        {
            EditorLayoutTools.SetLabelWidth(80f);

            if (atlasTexture == null)
            {
                EditorGUILayout.HelpBox("No AtlasTexture selected.", MessageType.Info);
            }
            else
            {
                GUILayout.Space(15f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(10f);

                    EditorGUILayout.ObjectField(atlasTexture, typeof(AtlasTexture), false, GUILayout.MinWidth(250f), GUILayout.MaxWidth(500f));

                    GUILayout.FlexibleSpace();

                    using (new EditorGUILayout.HorizontalScope())
                    {

                        EditorGUI.BeginChangeCheck();

                        spriteSearchText = EditorGUILayout.TextField(string.Empty, spriteSearchText, "SearchTextField", GUILayout.Width(200f));

                        if (EditorGUI.EndChangeCheck())
                        {
                            spriteNames = GetListOfSprites(spriteSearchText);
                        }

                        if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                        {
                            spriteSearchText = null;
                            spriteNames = GetListOfSprites(null);

                            GUIUtility.keyboardControl = 0;
                        }
                    }

                }

                EditorGUILayout.Separator();

                var spriteNameStyle = new GUIStyle("ProgressBarBack");

                spriteNameStyle.alignment = TextAnchor.MiddleCenter;
                spriteNameStyle.wordWrap = true;

                var contentWidth = 95f;

                var index = 0;
                var windowWidth = Screen.width;
                GUILayout.Space(10f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(15f);

                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                    {
                        while (index < spriteNames.Length)
                        {
                            var currentWidth = 0f;

                            using (new EditorGUILayout.VerticalScope())
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    while (index < spriteNames.Length)
                                    {
                                        if (windowWidth < currentWidth + contentWidth)
                                        {
                                             break;
                                        }

                                        var sprite = atlasTexture.GetSprite(spriteNames[index++]);

                                        if (sprite == null) { continue; }

                                        var imageSize = 85f;

                                        var texture = sprite.texture;

                                        using (new EditorGUILayout.VerticalScope(GUILayout.Width(contentWidth)))
                                        {
                                            if (GUILayout.Button(string.Empty, GUIStyle.none, GUILayout.Width(imageSize), GUILayout.Height(imageSize)))
                                            {
                                                AtlasTextureInspector.RepaintSprites();

                                                if (onSelectSprite != null)
                                                {
                                                    onSelectSprite.OnNext(sprite.name);
                                                    Close();
                                                }
                                            }

                                            var imageRect = GUILayoutUtility.GetLastRect();

                                            imageRect.xMin += 5f;
                                            imageRect.xMax += 5f;

                                            var uvRect = new Rect()
                                            {
                                                xMin = sprite.rect.xMin / texture.width,
                                                xMax = sprite.rect.xMax / texture.width,
                                                yMin = sprite.rect.yMin / texture.height,
                                                yMax = sprite.rect.yMax / texture.height,
                                            };

                                            EditorLayoutTools.DrawTexture(imageRect, imageSize, texture, uvRect);

                                            var selection = selectionSpriteName == sprite.name;

                                            if (selection)
                                            {
                                                EditorLayoutTools.DrawOutline(imageRect, new Color(0.4f, 1f, 0f, 1f));
                                            }

                                            using (new BackgroundColorScope(selection ? new Color(0.4f, 1f, 0f, 0.35f) : new Color(1f, 1f, 1f, 0.5f)))
                                            {
                                                using (new ContentColorScope(new Color(1f, 1f, 1f, 0.7f)))
                                                {
                                                    using (new EditorGUILayout.HorizontalScope())
                                                    {
                                                        EditorGUILayout.LabelField(sprite.name, spriteNameStyle, GUILayout.Width(imageSize));
                                                    }
                                                }
                                            }                                            
                                        }
                                        
                                        currentWidth += contentWidth;

                                        if (spriteNames.Length <= index)
                                        {
                                            GUILayout.FlexibleSpace();
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        scrollPosition = scrollViewScope.scrollPosition;
                    }

                    GUILayout.Space(5f);
                }
            }
        }

        public string[] GetListOfSprites(string searchText)
        {
            var spriteNames = atlasTexture.SpriteData.Select(x => x.SpriteName).ToArray();

            if (string.IsNullOrEmpty(searchText)) { return spriteNames; }

            var list = new List<string>();

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var spriteName in spriteNames)
            {
                if (spriteName.IsMatch(keywords))
                {
                    list.Add(spriteName);
                }
            }

            return list.ToArray();
        }

        public IObservable<string> OnSelectSpriteAsObservable()
        {
            return onSelectSprite ?? (onSelectSprite = new Subject<string>());
        }
    }
}
