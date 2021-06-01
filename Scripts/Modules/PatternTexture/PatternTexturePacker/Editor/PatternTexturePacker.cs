
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Generators;
using Modules.Devkit.Prefs;

namespace Modules.PatternTexture
{
	public sealed class PatternTexturePacker : SingletonEditorWindow<PatternTexturePacker>
	{
        //----- params -----

        private static class Prefs
        {
            public static string exportPath
            {
                get { return ProjectPrefs.GetString("PatternTexturePackerPrefs-exportPath", null); }
                set { ProjectPrefs.SetString("PatternTexturePackerPrefs-exportPath", value); }
            }
        }

        private readonly Vector2 WindowSize = new Vector2(400f, 350f);

        private readonly int[] BlockSizes = new int[] { 16, 32, 64, 128 };

        private const int DefaultBlockSize = 32;
        private const int DefaultPadding = 1;

        private const int MB = 1024 * 1024;

        private enum TextureStatus
        {
            None = 0,

            // 既存.
            Exist, 
            // 新規.
            Add,
            // 更新.
            Update,
            // 行方不明.
            Missing,
        }

        private sealed class TextureInfo
        {
            public TextureStatus status;
            public Texture2D texture;
        }

        //----- field -----

        private PatternTexture selectPatternTexture = null;
        private PatternTextureGenerator generator = null;

        private int blockSize = DefaultBlockSize;
        private int padding = DefaultPadding;
        private bool hasAlphaMap = false;
        private FilterMode filterMode = FilterMode.Bilinear;
        private string selectionTextureName = null;
        private TextureInfo[] textureInfos = null;
        private List<string> deleteNames = new List<string>();
        private Vector2 scrollPosition = Vector2.zero;

        private bool calcPerformance = false;
        private float totalMemSize = 0f;
        private float totalAtlasMemSize = 0f;
        private float totalFileSize = 0f;
        private float atlasFileSize = 0f;
        private float infoFileSize = 0f;

        private bool initialized = false;

        public static PatternTexturePacker instance = null;

        //----- property -----

        //----- method -----
        
        public static void Open()
        {
            if(!IsExist)
            {
                Instance.Initialize();
                instance.Show();
            }
        }

        private void Initialize()
        {
            if(initialized) { return; }

            titleContent = new GUIContent("PatternTexturePacker");
            minSize = WindowSize;

            generator = new PatternTextureGenerator();

            initialized = true;
        }

        void OnEnable()
        {
            instance = this;
            Selection.selectionChanged += OnSelectionUpdate;
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionUpdate;
            instance = null;
        }

        private static void OnSelectionUpdate()
        {
            if(instance == null) { return; }

            var selectionTextures = GetSelectionTextures();

            instance.BuildTextureInfos(selectionTextures);
        }

        void OnGUI()
        {
            var selectionTextures = Selection.objects != null ? Selection.objects.OfType<Texture2D>().ToArray() : null;

            GUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();

            selectPatternTexture = EditorLayoutTools.ObjectField(selectPatternTexture, false);

            if (EditorGUI.EndChangeCheck())
            {
                if(selectPatternTexture != null)
                {
                    blockSize = selectPatternTexture.BlockSize;
                    padding = selectPatternTexture.Padding;

                    Selection.objects = new UnityEngine.Object[0];

                    BuildTextureInfos(null);
                }
                else
                {
                    blockSize = DefaultBlockSize;
                    padding = DefaultPadding;
                }

                deleteNames.Clear();
            }

            GUILayout.Space(5f);

            DrawSettingsGUI();

            GUILayout.Space(5f);

            if (selectPatternTexture == null)
            {
                DrawCreateGUI(selectionTextures);
            }
            else
            {
                DrawUpdateGUI(selectPatternTexture);
            }
        }

        public void BuildTextureInfos(Texture2D[] addTextures)
        {
            // パック済みのテクスチャは除外.
            if (selectPatternTexture != null && addTextures != null)
            {
                addTextures = addTextures
                    .Where(x => x != selectPatternTexture.Texture)
                    .ToArray();
            }

            var allPatternData = selectPatternTexture != null ? 
                selectPatternTexture.GetAllPatternData() :
                new PatternData[0];

            var textureInfoByGuid = new Dictionary<string, TextureInfo>();

            foreach (var item in allPatternData)
            {
                if(string.IsNullOrEmpty(item.Guid)) { continue; }

                var assetPath = AssetDatabase.GUIDToAssetPath(item.Guid);
                var texture = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture2D;

                var info = new TextureInfo();

                info.status = texture != null ? TextureStatus.Exist : TextureStatus.Missing;

                if (info.status == TextureStatus.Exist)
                {
                    var fullPath = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);
                    var lastUpdate = File.GetLastWriteTime(fullPath).ToUnixTime();

                    if (item.LastUpdate != lastUpdate)
                    {
                        info.status = TextureStatus.Update;
                    }

                    info.texture = texture;
                }

                textureInfoByGuid.Add(item.Guid, info);
            }

            if (addTextures != null)
            {
                foreach (var texture in addTextures)
                {
                    var assetPath = AssetDatabase.GetAssetPath(texture);
                    var guid = AssetDatabase.AssetPathToGUID(assetPath);

                    var info = textureInfoByGuid.GetValueOrDefault(guid);

                    if (info == null)
                    {
                        info = new TextureInfo()
                        {
                            status = TextureStatus.Add,
                            texture = texture,
                        };

                        textureInfoByGuid.Add(guid, info);
                    }
                    else
                    {
                        textureInfoByGuid[guid].status = TextureStatus.Update;
                    }
                }
            }

            textureInfos = textureInfoByGuid.Values.ToArray();

            CalcPerformance(selectPatternTexture);

            Repaint();
        }

        private void DrawSettingsGUI()
        {
            var labelWidth = 80f;

            EditorLayoutTools.Title("Settings", EditorLayoutTools.BackgroundColor, EditorLayoutTools.LabelColor);

            using (new EditorGUILayout.HorizontalScope())
            {
                var labels = BlockSizes.Select(x => x.ToString()).ToArray();

                GUILayout.Label("BlockSize", GUILayout.Width(labelWidth));
                blockSize = EditorGUILayout.IntPopup(blockSize, labels, BlockSizes, GUILayout.Width(75f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Padding", GUILayout.Width(labelWidth));
                padding = Mathf.Clamp(EditorGUILayout.IntField(padding, GUILayout.Width(75f)), 1, 5);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("AlphaMap", GUILayout.Width(labelWidth));
                hasAlphaMap = EditorGUILayout.Toggle(hasAlphaMap, GUILayout.Width(75f));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("FilterMode", GUILayout.Width(labelWidth));
                filterMode = (FilterMode)EditorGUILayout.EnumPopup(filterMode, GUILayout.Width(75f));
            }
        }

        private void DrawCreateGUI(Texture2D[] selectionTextures)
        {
            var defaultBackgroundColor = GUI.backgroundColor;

            EditorGUILayout.HelpBox("対象のテクスチャを選択してください。", MessageType.Info);

            using (new DisableScope(selectionTextures.IsEmpty()))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(20f);

                    GUI.backgroundColor = selectionTextures.Any() ? Color.yellow : Color.white;

                    if (GUILayout.Button("Create"))
                    {
                        GeneratePatternTexture(null);
                    }

                    GUI.backgroundColor = defaultBackgroundColor;

                    GUILayout.Space(20f);
                }
            }
        }

        private void DrawUpdateGUI(PatternTexture patternTexture)
        {
            var labelStyle = new GUIStyle(EditorStyles.label);

            var defaultColor = labelStyle.normal.textColor;
            var defaultBackgroundColor = GUI.backgroundColor;

            var delete = false;

            if (textureInfos.Any())
            {
                EditorLayoutTools.Title("Sprites", EditorLayoutTools.BackgroundColor, EditorLayoutTools.LabelColor);

                EditorGUILayout.Separator();

                using (new EditorGUILayout.VerticalScope())
                {
                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                    {
                        int index = 0;

                        for (var i = 0; i < textureInfos.Length; i++)
                        {
                            ++index;

                            GUILayout.Space(-1f);

                            var textureName = textureInfos[i].texture != null ? textureInfos[i].texture.name : null;

                            var highlight = selectionTextureName == textureName;

                            GUI.backgroundColor = highlight ? Color.white : new Color(0.8f, 0.8f, 0.8f);

                            using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea, GUILayout.MinHeight(20f)))
                            {
                                GUI.backgroundColor = Color.white;
                                GUILayout.Label(index.ToString(), GUILayout.Width(24f));

                                if (GUILayout.Button(textureName, EditorStyles.label, GUILayout.Height(20f)))
                                {
                                    selectionTextureName = textureName;
                                }

                                switch (textureInfos[i].status)
                                {
                                    case TextureStatus.Add:
                                        labelStyle.normal.textColor = Color.green;
                                        GUILayout.Label("Add", labelStyle, GUILayout.Width(27f));
                                        break;  
                                        
                                    case TextureStatus.Update:
                                        labelStyle.normal.textColor = Color.cyan;
                                        GUILayout.Label("Update", labelStyle, GUILayout.Width(45f));
                                        break; 
                                    case TextureStatus.Missing:
                                        labelStyle.normal.textColor = Color.yellow;
                                        GUILayout.Label("Missing", labelStyle, GUILayout.Width(45f));
                                        break;
                                }

                                labelStyle.normal.textColor = defaultColor;

                                if (deleteNames.Contains(textureName))
                                {
                                    GUI.backgroundColor = Color.red;

                                    if (GUILayout.Button("Delete", GUILayout.Width(60f)))
                                    {
                                        delete = true;
                                    }

                                    GUI.backgroundColor = Color.green;

                                    if (GUILayout.Button("X", GUILayout.Width(22f)))
                                    {
                                        deleteNames.Remove(textureName);
                                    }
                                }
                                else
                                {
                                    if (GUILayout.Button("X", GUILayout.Width(22f)))
                                    {
                                        if (!deleteNames.Contains(textureName))
                                        {
                                            deleteNames.Add(textureName);
                                        }
                                    }
                                }

                                GUILayout.Space(5f);
                            }
                        }

                        scrollPosition = scrollViewScope.scrollPosition;
                    }
                }
            }
            
            if(calcPerformance)
            {
                GUILayout.Space(5f);

                EditorLayoutTools.Title("Result", EditorLayoutTools.BackgroundColor, EditorLayoutTools.LabelColor);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("MemorySize", GUILayout.Width(75f));

                    var memSize = infoFileSize + totalAtlasMemSize;

                    labelStyle.normal.textColor = totalMemSize < memSize ? Color.red : defaultColor;
                    GUILayout.Label(string.Format("{0:F1} MB >>> {1:F1} MB : {2:F1}% ", totalMemSize, memSize, 100.0f * memSize / totalMemSize), labelStyle);
                    labelStyle.normal.textColor = defaultColor;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("FileSize", GUILayout.Width(75f));

                    var fileSize = infoFileSize + atlasFileSize;

                    labelStyle.normal.textColor = totalFileSize < atlasFileSize ? Color.red : defaultColor;
                    GUILayout.Label(string.Format("{0:F1} MB >>> {1:F1} MB : {2:F1}% ", totalFileSize, fileSize, 100.0f * fileSize / totalFileSize), labelStyle);
                    labelStyle.normal.textColor = defaultColor;
                }
            }

            GUILayout.Space(15f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.backgroundColor = Color.cyan;

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Generate", GUILayout.Width(150f)))
                {
                    GeneratePatternTexture(patternTexture);
                }

                GUI.backgroundColor = defaultBackgroundColor;

                GUILayout.Space(25f);

                if (GUILayout.Button("View Textures", GUILayout.Width(150f)))
                {
                    Action<string> onSelection = x =>
                    {
                        selectionTextureName = x;
                        Repaint();
                    };

                    PatternSpriteSelector.Show(patternTexture, selectionTextureName, onSelection, null);
                }

                GUILayout.FlexibleSpace();
            }

            if(delete)
            {
                GeneratePatternTexture(patternTexture);
            }

            GUILayout.Space(5f);
        }

        private void CalcPerformance(PatternTexture patternTexture)
        {
            calcPerformance = false;

            if (patternTexture != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(patternTexture);
                var fullPath = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);

                var fileInfo = new FileInfo(fullPath);

                infoFileSize = (float)fileInfo.Length / MB;
            }
            else
            {
                return;
            }

            var textures = patternTexture.GetAllPatternData()
                .Select(x => AssetDatabase.GUIDToAssetPath(x.Guid))
                .Select(x => AssetDatabase.LoadMainAssetAtPath(x) as Texture2D)
                .Where(x => x != null)
                .ToArray();

            // 消費メモリサイズを計測.
            totalMemSize = 0;
            textures.ForEach(x =>
                {
                    var mem = Mathf.NextPowerOfTwo(x.width) * Mathf.NextPowerOfTwo(x.height);
                    mem *= !x.alphaIsTransparency ? 3 : 4;
                    totalMemSize += mem;
                });
            totalMemSize /= MB;

            if (patternTexture.Texture != null)
            {
                var mem = Mathf.NextPowerOfTwo(patternTexture.Texture.width) * Mathf.NextPowerOfTwo(patternTexture.Texture.height);
                mem *= !patternTexture.Texture.alphaIsTransparency ? 3 : 4;
                totalAtlasMemSize = (float)mem / MB;
            }

            // ファイルサイズ.
            totalFileSize = 0f;
            textures.Select(x => AssetDatabase.GetAssetPath(x))
                .Select(x => UnityPathUtility.ConvertAssetPathToFullPath(x))
                .Select(x => new FileInfo(x))
                .ForEach(x => totalFileSize += (float)x.Length / MB);

            if (patternTexture.Texture != null)
            {
                var assetPath = AssetDatabase.GetAssetPath(patternTexture.Texture);
                var fullPath = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);

                var fileInfo = new FileInfo(fullPath);

                atlasFileSize = (float)fileInfo.Length / MB;
            }

            calcPerformance = true;
        }

        private void GeneratePatternTexture(PatternTexture patternTexture)
        {
            if (textureInfos == null)
            {
                Debug.LogError("Require select texture.");
                return;
            }

            var exportPath = string.Empty;

            var textures = textureInfos
                .Where(x => x.status == TextureStatus.Exist ||
                            x.status == TextureStatus.Add ||
                            x.status == TextureStatus.Update)
                .Where(x => !deleteNames.Contains(x.texture.name))
                .Select(x => x.texture)
                .ToArray();

            if (patternTexture == null)
            {
                var path = string.Empty;

                var savedExportPath = Prefs.exportPath;

                if (string.IsNullOrEmpty(savedExportPath) || !File.Exists(savedExportPath))
                {
                    path = UnityPathUtility.AssetsFolder;
                }
                else
                {
                    var directory = Directory.GetParent(savedExportPath);

                    path = directory.FullName;
                }

                exportPath = EditorUtility.SaveFilePanelInProject("Save As", "New PatternTexture.asset", "asset", "Save as...", path);
            }
            else
            {
                exportPath = AssetDatabase.GetAssetPath(patternTexture);
            }

            if (!string.IsNullOrEmpty(exportPath))
            {
                patternTexture = ScriptableObjectGenerator.Generate<PatternTexture>(exportPath);

                var patternData = generator.Generate(exportPath, blockSize, padding, textures, hasAlphaMap);

                patternTexture.Set(patternData.Texture, blockSize, padding, patternData.PatternData, patternData.PatternBlocks, hasAlphaMap);
                patternTexture.Texture.filterMode = filterMode;

                UnityEditorUtility.SaveAsset(patternTexture);

                Prefs.exportPath = exportPath;

                selectPatternTexture = patternTexture;

                var selectionTextures = GetSelectionTextures();

                BuildTextureInfos(selectionTextures);

                deleteNames.Clear();

                Repaint();
            }
        }

        private static Texture2D[] GetSelectionTextures()
        {
            var selectionObjects = Selection.objects;

            if (selectionObjects == null) { return new Texture2D[0]; }

            return selectionObjects.OfType<Texture2D>().ToArray();
        }
    }
}
