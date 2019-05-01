
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

namespace Modules.SpriteSheet
{
    public partial class SpriteSheetMaker : EditorWindow
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
        public enum MakerAction
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
                get { return ProjectPrefs.GetString("SpriteSheetMakerPrefs-exportPath", null); }
                set { ProjectPrefs.SetString("SpriteSheetMakerPrefs-exportPath", value); }
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

        private SpriteSheet selectSpriteSheet = null;

        private Vector2 scrollPos = Vector2.zero;
        private string selection = null;

        private List<string> deleteNames = new List<string>();

        private Color defaultColor = new Color();
        private Color defaultBackgroundColor = new Color();

        public static SpriteSheetMaker instance = null;

        //----- property -----

        //----- method -----

        public static void Open()
        {
            if (instance != null)
            {
                instance.Close();
            }

            instance = EditorWindow.GetWindow<SpriteSheetMaker>(false, "SpriteSheetMaker", true);
            instance.Initialize();
        }

        public void Initialize()
        {
            titleContent = new GUIContent("SpriteSheetMaker");
            minSize = WindowSize;

            defaultColor = GUI.color;
            defaultBackgroundColor = GUI.backgroundColor;

            Show();
        }

        void OnEnable() { instance = this; }

        void OnDisable() { instance = null; }

        void OnGUI()
        {
            var action = MakerAction.None;

            var textures = GetSelectedTextures(selectSpriteSheet);

            var spriteInfos = GetSpriteInfos(textures);

            ComponentSelector.Draw("Asset", selectSpriteSheet, OnSelectSpriteSheet, GUILayout.MinWidth(80f));

            if (selectSpriteSheet == null)
            {
                action |= DrawSpriteSheetCreate(textures);
            }
            else
            {
                DrawSpriteSheetInfos();
                DrawOptions();

                action |= DrawSpriteList(spriteInfos);
                action |= DrawButtons();
            }

            if (action != MakerAction.None)
            {
                ApplySpriteSheet(action, textures);
            }

            GUI.color = defaultColor;
            GUI.backgroundColor = defaultBackgroundColor;
        }

        void OnSelectionChange()
        {
            Repaint();
        }

        #region Draw GUI

        private void DrawSpriteSheetInfos()
        {
            EditorLayoutTools.DrawLabelWithBackground("Input", EditorLayoutTools.BackgroundColor, EditorLayoutTools.LabelColor);

            var tex = selectSpriteSheet.Texture;

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

        private MakerAction DrawButtons()
        {
            var action = MakerAction.None;

            GUILayout.Space(5f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = Color.cyan;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Apply", GUILayout.Width(150f)))
                {
                    action |= MakerAction.Update;
                }

                GUI.backgroundColor = defaultBackgroundColor;

                GUILayout.Space(25f);

                if (GUILayout.Button("View Sprites", GUILayout.Width(150f)))
                {
                    SpriteSelector.ShowSelected();
                }

                GUILayout.FlexibleSpace();
            }

            GUILayout.Space(5f);

            return action;
        }

        private MakerAction DrawSpriteSheetCreate(List<Texture> textures)
        {
            var action = MakerAction.None;

            EditorGUILayout.HelpBox("パック対象のテクスチャを選択してください。", MessageType.Info);

            using (new DisableScope(textures.Count == 0))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(20f);
                    GUI.backgroundColor = textures.Count != 0 ? Color.yellow : Color.white;
                    if (GUILayout.Button("Create"))
                    {
                        action = MakerAction.Create | MakerAction.Replace;
                    }
                    GUI.backgroundColor = defaultBackgroundColor;
                    GUILayout.Space(20f);
                }
            }

            return action;
        }

        private MakerAction DrawSpriteList(SpriteInfo[] spriteInfos)
        {
            var action = MakerAction.None;

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

                            var highlight = (SpriteSheetInspector.instance != null) && (EditorSpriteSheetPrefs.selectedSprite == spriteInfo.name);

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
                                    action |= MakerAction.Delete;
                                }
                            }
                        }

                        scrollPos = scrollViewScope.scrollPosition;
                    }
                }
            }

            if (EditorSpriteSheetPrefs.spriteSheet != null && !string.IsNullOrEmpty(selection))
            {
                SpriteSheetInspector.SelectSprite(selection);
                selection = null;
            }

            return action;
        }

        #endregion

        private void ApplySpriteSheet(MakerAction action, List<Texture> textures)
        {
            if (action == MakerAction.None) { return; }

            StartTextureEdit(selectSpriteSheet, textures);

            if (action.HasFlag(MakerAction.Create))
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

                path = EditorUtility.SaveFilePanelInProject("Save As", "New SpriteSheet.asset", "asset", "Save spritesheet as...", path);

                if (!string.IsNullOrEmpty(path))
                {
                    Prefs.exportPath = Path.GetDirectoryName(path) + PathUtility.PathSeparator;

                    // Create ScriptableObject for spritesheet.
                    var spritSheet = ScriptableObjectGenerator.Generate<SpriteSheet>(path, false);

                    if (spritSheet != null)
                    {
                        Selection.activeObject = spritSheet;

                        OnSelectSpriteSheet(spritSheet);
                    }
                }
                else
                {
                    action = MakerAction.None;
                }
            }
            else if (action.HasFlag(MakerAction.Delete))
            {
                var sprites = new List<SpriteEntry>();
                ExtractSprites(selectSpriteSheet, sprites);

                for (int i = sprites.Count; i > 0;)
                {
                    var ent = sprites[--i];

                    if (deleteNames.Contains(ent.name))
                    {
                        sprites.RemoveAt(i);
                    }
                }

                UpdateSpriteSheet(selectSpriteSheet, sprites);

                deleteNames.Clear();
            }

            if (action.HasFlag(MakerAction.Update))
            {
                UpdateSpriteSheet(selectSpriteSheet, textures, true);
            }
            else if (action.HasFlag(MakerAction.Replace))
            {
                UpdateSpriteSheet(selectSpriteSheet, textures, false);
            }

            selectSpriteSheet.CacheClear();

            FinishTextureEdit();

            UnityEditorUtility.SaveAsset(selectSpriteSheet);
        }

        private void OnSelectSpriteSheet(UnityEngine.Object obj)
        {
            if (EditorSpriteSheetPrefs.spriteSheet != obj || obj == null)
            {
                EditorSpriteSheetPrefs.spriteSheet = obj as SpriteSheet;

                if (EditorSpriteSheetPrefs.spriteSheet != null)
                {
                    selectSpriteSheet = EditorSpriteSheetPrefs.spriteSheet;
                    padding = selectSpriteSheet.Padding;
                    pixelsPerUnit = selectSpriteSheet.PixelsPerUnit;
                    filterMode = selectSpriteSheet.FilterMode;
                }
                else
                {
                    selectSpriteSheet = null;
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

            if (selectSpriteSheet != null)
            {
                var sprites = selectSpriteSheet.Sprites;

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

        private List<Texture> GetSelectedTextures(SpriteSheet spriteSheet)
        {
            var textures = new List<Texture>();

            if (Selection.objects != null && Selection.objects.Length > 0)
            {
                foreach (var o in Selection.objects)
                {
                    var tex = o as Texture;

                    if (tex == null) { continue; }

                    if (tex.name == "Font Texture") { continue; }

                    if (spriteSheet != null && spriteSheet.Texture != null)
                    {
                        if (tex.GetInstanceID() == spriteSheet.Texture.GetInstanceID()) { continue; }
                    }

                    textures.Add(tex);
                }
            }

            return textures;
        }

        public string GetSaveableTexturePath(SpriteSheet spriteSheet)
        {
            const string TextureExtension = ".png";

            string path = "";

            if (spriteSheet.Texture != null)
            {
                path = AssetDatabase.GetAssetPath(spriteSheet.Texture.GetInstanceID());

                if (!string.IsNullOrEmpty(path))
                {
                    var dot = path.LastIndexOf('.');

                    return path.Substring(0, dot) + TextureExtension;
                }
            }

            path = AssetDatabase.GetAssetPath(spriteSheet.GetInstanceID());
            path = string.IsNullOrEmpty(path) ? "Assets/" + spriteSheet.name + TextureExtension : path.Replace(".asset", TextureExtension);

            return path;
        }
    }
}
