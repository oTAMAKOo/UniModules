
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Generators;
using Modules.Devkit.Prefs;

namespace Modules.Atlas
{
    public partial class AtlasPacker : EditorWindow
    {
        //----- params -----

        private readonly Vector2 WindowSize = new Vector2(400f, 200f);

        public enum SpriteAction
        {
            None = 0,
            Add,
            Update,
            Delete,
        };

        [Flags]
        public enum AtlasAction
        {
            None = 1 << 0,
            Create = 1 << 1,
            Update = 1 << 2,
            Replace = 1 << 3,
            Delete = 1 << 4,
        }

        private static class Prefs
        {
            public static string exportPath
            {
                get { return ProjectPrefs.GetString("SpritePackerPrefs-exportPath", null); }
                set { ProjectPrefs.SetString("SpritePackerPrefs-exportPath", value); }
            }
        }

        private class SpriteInfo
        {
            public string name = null;
            public string guid = null;
            public SpriteAction action = SpriteAction.None;

            public SpriteInfo(SpriteData data, SpriteAction action)
            {
                this.name = data.name;
                this.guid = data.guid;
                this.action = action;
            }

            public SpriteInfo(Texture texture, SpriteAction action)
            {
                this.name = texture.name;
                this.guid = UnityEditorUtility.GetAssetGUID(texture);
                this.action = action;
            }
        }

        //----- field -----

        private AtlasTexture selectAtlas = null;

        private Vector2 scrollPos = Vector2.zero;
        private string selection = null;

        private List<string> deleteNames = new List<string>();

        private Color defaultColor = new Color();
        private Color defaultBackgroundColor = new Color();

        public static AtlasPacker instance = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            if (instance != null)
            {
                instance.Close();
            }

            instance = EditorWindow.GetWindow<AtlasPacker>(false, "SpritePacker", true);
            instance.Initialize();
        }

        public void Initialize()
        {
            titleContent = new GUIContent("AtlasPacker");
            minSize = WindowSize;

            defaultColor = GUI.color;
            defaultBackgroundColor = GUI.backgroundColor;

            Show();
        }

        void OnEnable() { instance = this; }

        void OnDisable() { instance = null; }

        void OnGUI()
        {
            var action = AtlasAction.None;

            var textures = GetSelectedTextures(selectAtlas);

            var spriteInfos = GetSpriteInfos(textures);

            ComponentSelector.Draw("Atlas", selectAtlas, OnSelectAtlas, GUILayout.MinWidth(80f));

            if (selectAtlas == null)
            {
                action |= DrawAtlasCreate(textures);
            }
            else
            {
                DrawAtlasInfos();
                DrawOptions();

                action |= DrawSpriteList(spriteInfos);
                action |= DrawButtons();
            }

            if (action != AtlasAction.None)
            {
                ApplyAtlas(action, textures);
            }

            GUI.color = defaultColor;
            GUI.backgroundColor = defaultBackgroundColor;
        }

        void OnSelectionChange()
        {
            Repaint();
        }

        #region Draw GUI

        private void DrawAtlasInfos()
        {
            EditorLayoutTools.DrawLabelWithBackground("Input", EditorLayoutTools.BackgroundColor, EditorLayoutTools.LabelColor);

            var tex = selectAtlas.Texture;

            var labelWidth = 80f;

            using (new EditorGUILayout.HorizontalScope())
            {
                if (tex != null)
                {
                    if (GUILayout.Button("Texture", GUILayout.Width(labelWidth)))
                    {
                        Selection.activeObject = tex;
                    }

                    GUILayout.Label(" " + tex.width + "x" + tex.height);
                }
                else
                {
                    GUI.color = Color.grey;
                    GUILayout.Button("Texture", GUILayout.Width(labelWidth));
                    GUI.color = Color.white;
                    GUILayout.Label(" N/A");
                }
            }
        }

        private void DrawOptions()
        {
            var labelWidth = 80f;
            
            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Padding", GUILayout.Width(labelWidth));
                padding = Mathf.Clamp(EditorGUILayout.IntField(padding, GUILayout.Width(50f)), 0, 10);
            }

            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("PixelsPerUnit", GUILayout.Width(labelWidth));
                pixelsPerUnit = EditorGUILayout.FloatField(pixelsPerUnit, GUILayout.Width(50f));
            }

            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("FilterMode", GUILayout.Width(labelWidth));
                filterMode = (FilterMode)EditorGUILayout.EnumPopup(filterMode, GUILayout.Width(80f));
            }

            GUILayout.Space(5f);
        }

        private AtlasAction DrawButtons()
        {
            var action = AtlasAction.None;

            GUILayout.Space(5f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = Color.cyan;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Apply", GUILayout.Width(150f)))
                {
                    action |= AtlasAction.Update;
                }

                GUI.backgroundColor = defaultBackgroundColor;

                GUILayout.Space(25f);

                if (GUILayout.Button("View Sprites", GUILayout.Width(150f)))
                {
                    AtlasSpriteSelector.ShowSelected();
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(5f);

            return action;
        }

        private AtlasAction DrawAtlasCreate(List<Texture> textures)
        {
            var action = AtlasAction.None;

            EditorGUILayout.HelpBox("パック対象のテクスチャを選択してください。", MessageType.Info);

            using (new DisableScope(textures.Count == 0))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(20f);
                    GUI.backgroundColor = textures.Count != 0 ? Color.yellow : Color.white;
                    if (GUILayout.Button("Create"))
                    {
                        action = AtlasAction.Create | AtlasAction.Replace;
                    }
                    GUI.backgroundColor = defaultBackgroundColor;
                    GUILayout.Space(20f);
                }
            }

            return action;
        }

        private AtlasAction DrawSpriteList(SpriteInfo[] spriteInfos)
        {
            var action = AtlasAction.None;

            if (spriteInfos.Any())
            {
                EditorLayoutTools.DrawLabelWithBackground("Sprites", EditorLayoutTools.BackgroundColor, EditorLayoutTools.LabelColor);

                EditorGUILayout.Separator();

                using (new EditorGUILayout.VerticalScope())
                {
                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPos))
                    {
                        var delete = false;

                        int index = 0;

                        foreach (var spriteInfo in spriteInfos)
                        {
                            ++index;

                            GUILayout.Space(-1f);

                            var highlight = (AtlasTextureInspector.instance != null) && (EditorAtlasPrefs.selectedSprite == spriteInfo.name);

                            GUI.backgroundColor = highlight ? Color.white : new Color(0.8f, 0.8f, 0.8f);

                            using (new EditorGUILayout.HorizontalScope(EditorLayoutTools.TextAreaStyle, GUILayout.MinHeight(20f)))
                            {
                                GUI.backgroundColor = Color.white;
                                GUILayout.Label(index.ToString(), GUILayout.Width(24f));

                                if (GUILayout.Button(spriteInfo.name, EditorStyles.label, GUILayout.Height(20f)))
                                {
                                    selection = spriteInfo.name;
                                }

                                if (spriteInfo.action == SpriteAction.Add)
                                {
                                    GUI.color = Color.green;
                                    GUILayout.Label("Add", GUILayout.Width(27f));
                                    GUI.color = defaultColor;
                                }
                                else if (spriteInfo.action == SpriteAction.Update)
                                {
                                    GUI.color = Color.cyan;
                                    GUILayout.Label("Update", GUILayout.Width(45f));
                                    GUI.color = defaultColor;
                                }
                                else
                                {
                                    if (deleteNames.Contains(spriteInfo.name))
                                    {
                                        GUI.backgroundColor = Color.red;

                                        if (GUILayout.Button("Delete", GUILayout.Width(60f)))
                                        {
                                            delete = true;
                                        }

                                        GUI.backgroundColor = Color.green;

                                        if (GUILayout.Button("X", GUILayout.Width(22f)))
                                        {
                                            deleteNames.Remove(spriteInfo.name);
                                            delete = false;
                                        }

                                    }
                                    else
                                    {
                                        if (GUILayout.Button("X", GUILayout.Width(22f)))
                                        {
                                            if (!deleteNames.Contains(spriteInfo.name))
                                            {
                                                deleteNames.Add(spriteInfo.name);
                                            }
                                        }
                                    }
                                }

                                GUILayout.Space(5f);

                                if (delete)
                                {
                                    action |= AtlasAction.Delete;
                                }
                            }
                        }

                        scrollPos = scrollViewScope.scrollPosition;
                    }
                }
            }

            if (EditorAtlasPrefs.atlas != null && !string.IsNullOrEmpty(selection))
            {
                AtlasTextureInspector.SelectSprite(selection);
                selection = null;
            }

            return action;
        }

        #endregion

        private void ApplyAtlas(AtlasAction action, List<Texture> textures)
        {
            if (action == AtlasAction.None) { return; }

            StartTextureEdit(selectAtlas, textures);

            if (action.HasFlag(AtlasAction.Create))
            {
                var path = string.Empty;

                if (string.IsNullOrEmpty(Prefs.exportPath) || !Directory.Exists(path))
                {
                    path = UnityPathUtility.AssetsFolder;
                }
                else
                {
                    path = Prefs.exportPath;
                }

                path = EditorUtility.SaveFilePanelInProject("Save As", "New Atlas.asset", "asset", "Save atlas as...", path);

                if (!string.IsNullOrEmpty(path))
                {
                    Prefs.exportPath = Path.GetDirectoryName(path) + PathUtility.PathSeparator;

                    // Create ScriptableObject for atlas.
                    var atlasTexture = ScriptableObjectGenerator.Generate<AtlasTexture>(path, false);

                    if (atlasTexture != null)
                    {
                        Selection.activeObject = atlasTexture;

                        OnSelectAtlas(atlasTexture);
                    }
                }
                else
                {
                    action = AtlasAction.None;
                }
            }
            else if (action.HasFlag(AtlasAction.Delete))
            {
                var sprites = new List<SpriteEntry>();
                ExtractSprites(selectAtlas, sprites);

                for (int i = sprites.Count; i > 0;)
                {
                    var ent = sprites[--i];

                    if (deleteNames.Contains(ent.name))
                    {
                        sprites.RemoveAt(i);
                    }
                }

                UpdateAtlas(selectAtlas, sprites);

                deleteNames.Clear();
            }

            if (action.HasFlag(AtlasAction.Update))
            {
                UpdateAtlas(selectAtlas, textures, true);
            }
            else if (action.HasFlag(AtlasAction.Replace))
            {
                UpdateAtlas(selectAtlas, textures, false);
            }

            selectAtlas.CacheClear();

            FinishTextureEdit();

            UnityEditorUtility.SaveAsset(selectAtlas);
        }

        private void OnSelectAtlas(UnityEngine.Object obj)
        {
            if (EditorAtlasPrefs.atlas != obj || obj == null)
            {
                EditorAtlasPrefs.atlas = obj as AtlasTexture;

                if (EditorAtlasPrefs.atlas != null)
                {
                    selectAtlas = EditorAtlasPrefs.atlas;
                    padding = selectAtlas.Padding;
                    pixelsPerUnit = selectAtlas.PixelsPerUnit;
                    filterMode = selectAtlas.FilterMode;
                }
                else
                {
                    selectAtlas = null;
                    padding = 0;
                    filterMode = FilterMode.Bilinear;
                }

                Repaint();
            }
        }

        private SpriteInfo[] GetSpriteInfos(List<Texture> textures)
        {
            var list = new List<SpriteInfo>();

            if (textures.Any())
            {
                var textureArray = textures.OrderBy(x => x.name).ToArray();

                foreach (var item in textureArray)
                {
                    list.Add(new SpriteInfo(item, SpriteAction.Add));
                }
            }

            if (selectAtlas != null)
            {
                var sprites = selectAtlas.Sprites;

                foreach (var sprite in sprites)
                {
                    SpriteInfo info = null;

                    info = list.FirstOrDefault(x => !string.IsNullOrEmpty(x.guid) && x.guid == sprite.guid);

                    if (info == null)
                    {
                        info = list.FirstOrDefault(x => x.name == sprite.name);
                    }

                    if (info != null)
                    {
                        info.action = SpriteAction.Update;
                    }
                    else
                    {
                        list.Add(new SpriteInfo(sprite, SpriteAction.None));
                    }
                }
            }

            return list.ToArray();
        }

        private List<Texture> GetSelectedTextures(AtlasTexture atlas)
        {
            var textures = new List<Texture>();

            if (Selection.objects != null && Selection.objects.Length > 0)
            {
                foreach (var o in Selection.objects)
                {
                    var tex = o as Texture;

                    if (tex == null) { continue; }

                    if (tex.name == "Font Texture") { continue; }

                    if (atlas != null && atlas.Texture != null)
                    {
                        if (tex.GetInstanceID() == atlas.Texture.GetInstanceID()) { continue; }
                    }

                    textures.Add(tex);
                }
            }

            return textures;
        }

        public string GetSaveableTexturePath(AtlasTexture atlas)
        {
            const string TextureExtension = ".png";

            string path = "";

            if (atlas.Texture != null)
            {
                path = AssetDatabase.GetAssetPath(atlas.Texture.GetInstanceID());

                if (!string.IsNullOrEmpty(path))
                {
                    var dot = path.LastIndexOf('.');

                    return path.Substring(0, dot) + TextureExtension;
                }
            }

            path = AssetDatabase.GetAssetPath(atlas.GetInstanceID());
            path = string.IsNullOrEmpty(path) ? "Assets/" + atlas.name + TextureExtension : path.Replace(".asset", TextureExtension);

            return path;
        }
    }
}
