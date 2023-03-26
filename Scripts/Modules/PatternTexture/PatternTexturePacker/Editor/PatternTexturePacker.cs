
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
                get { return ProjectPrefs.GetString(typeof(Prefs).FullName + "-exportPath", null); }
                set { ProjectPrefs.SetString(typeof(Prefs).FullName + "-exportPath", value); }
            }
        }

        private readonly Vector2 WindowSize = new Vector2(400f, 350f);

        private readonly int[] BlockSizes = new int[] { 16, 32, 64, 128, 256 };

        private const int DefaultBlockSize = 64;
		private const int DefaultFilterPixels = 3;

		private const int BlockPadding = 1;

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
		private int filterPixels = DefaultFilterPixels;
		private PatternTexture.TextureSizeType sizeType = PatternTexture.TextureSizeType.SquarePowerOf2;
        private bool hasAlphaMap = false;
        private FilterMode filterMode = FilterMode.Bilinear;
        private string selectionTextureName = null;
        private TextureInfo[] textureInfos = null;
        private List<string> deleteNames = new List<string>();
        private Vector2 scrollPosition = Vector2.zero;

		private GUIContent deleteMarkIconContent = null;
		private GUIContent deleteIconContent = null;

		[NonSerialized]
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

			textureInfos = new TextureInfo[0];
			deleteNames = new List<string>();

			deleteMarkIconContent = null;
			deleteIconContent = null;

            generator = new PatternTextureGenerator();

			UpdateSelectPatternTextureInfo();

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

		private void LoadIconContent()
		{
			if (deleteMarkIconContent == null)
			{
				deleteMarkIconContent = EditorGUIUtility.IconContent("d_AS Badge Delete");
			}

			if (deleteIconContent == null)
			{
				deleteIconContent = EditorGUIUtility.IconContent("Toolbar Minus@2x");
			}
		}

		void OnGUI()
        {
			Initialize();

			LoadIconContent();

            var selectionTextures = Selection.objects != null ? Selection.objects.OfType<Texture2D>().ToArray() : null;

            GUILayout.Space(2f);

            EditorGUI.BeginChangeCheck();

            selectPatternTexture = EditorLayoutTools.ObjectField(selectPatternTexture, false);

            if (EditorGUI.EndChangeCheck())
            {
				UpdateSelectPatternTextureInfo();
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
				DrawPatternTextureInfo(selectPatternTexture);
				DrawButtons(selectPatternTexture);
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

            textureInfos = textureInfoByGuid.Values
				.OrderBy(x => AssetDatabase.GetAssetPath(x.texture), new NaturalComparer())
				.ToArray();

			Repaint();
        }

        private void DrawSettingsGUI()
        {
            var labelWidth = 120f;
			var contentWidth = 140f;

            EditorLayoutTools.Title("Settings", EditorLayoutTools.BackgroundColor, EditorLayoutTools.LabelColor);

			GUILayout.Space(3f);

            using (new EditorGUILayout.HorizontalScope())
            {
                var labels = BlockSizes.Select(x => x.ToString()).ToArray();

                GUILayout.Label("BlockSize", GUILayout.Width(labelWidth));
                blockSize = EditorGUILayout.IntPopup(blockSize, labels, BlockSizes, GUILayout.Width(contentWidth));
            }

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("FilterPixels", GUILayout.Width(labelWidth));
				filterPixels = Mathf.Clamp(EditorGUILayout.IntField(filterPixels, GUILayout.Width(contentWidth)), 1, 8);
			}

			using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("AlphaMap", GUILayout.Width(labelWidth));
                hasAlphaMap = EditorGUILayout.Toggle(hasAlphaMap, GUILayout.Width(contentWidth));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("FilterMode", GUILayout.Width(labelWidth));
                filterMode = (FilterMode)EditorGUILayout.EnumPopup(filterMode, GUILayout.Width(contentWidth));
            }

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("TextureSizeType", GUILayout.Width(labelWidth));
				sizeType = (PatternTexture.TextureSizeType)EditorGUILayout.EnumPopup(sizeType, GUILayout.Width(contentWidth));
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
			var e = Event.current;

			var defaultBackgroundColor = GUI.backgroundColor;

			var textureNameLabelStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleLeft,
				fontStyle = FontStyle.Bold,
			};

			var textureStatusLabelStyle = new GUIStyle(EditorStyles.label)
			{
				alignment = TextAnchor.MiddleCenter,
				fontStyle = FontStyle.Bold,
				fontSize = 10,
			};

            var defaultColor = textureStatusLabelStyle.normal.textColor;

			if (textureInfos.Any())
            {
				EditorLayoutTools.Title("Sprites", EditorLayoutTools.BackgroundColor, EditorLayoutTools.LabelColor);

				GUILayout.Space(2f);

                using (new EditorGUILayout.VerticalScope())
                {
                    using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                    {
                        for (var i = 0; i < textureInfos.Length; i++)
                        {
                            GUILayout.Space(-1f);

                            var textureName = textureInfos[i].texture != null ? textureInfos[i].texture.name : null;

                            var highlight = selectionTextureName == textureName;

                            GUI.backgroundColor = highlight ? Color.white : new Color(0.8f, 0.8f, 0.8f);
							
                            using (new EditorGUILayout.HorizontalScope(EditorStyles.textArea, GUILayout.MinHeight(18f)))
                            {
								GUI.backgroundColor = Color.white;

                                GUILayout.Space(4f);

								GUILayout.Label(textureName, textureNameLabelStyle, GUILayout.Height(18f));

								GUILayout.FlexibleSpace();

								var status = textureInfos[i].status;

								switch (status)
                                {
                                    case TextureStatus.Add:
										textureStatusLabelStyle.normal.textColor = Color.green;
                                        GUILayout.Label("Add", textureStatusLabelStyle, GUILayout.Width(27f), GUILayout.Height(18f));
                                        break;  
                                        
                                    case TextureStatus.Update:
										textureStatusLabelStyle.normal.textColor = Color.cyan;
                                        GUILayout.Label("Update", textureStatusLabelStyle, GUILayout.Width(45f), GUILayout.Height(18f));
                                        break; 
                                    case TextureStatus.Missing:
										textureStatusLabelStyle.normal.textColor = Color.yellow;
                                        GUILayout.Label("Missing", textureStatusLabelStyle, GUILayout.Width(45f), GUILayout.Height(18f));
                                        break;
                                }

								textureStatusLabelStyle.normal.textColor = defaultColor;

								if (status == TextureStatus.Exist || status == TextureStatus.Missing)
								{
	                                if (deleteNames.Contains(textureName))
	                                {
										GUILayout.Label(deleteMarkIconContent);

										using (new BackgroundColorScope(Color.green))
										{
											if (GUILayout.Button(deleteIconContent, EditorStyles.miniButton, GUILayout.Width(22f)))
											{
												deleteNames.Remove(textureName);
											}
										}
									}
	                                else
	                                {
	                                    if (GUILayout.Button(deleteIconContent, EditorStyles.miniButton, GUILayout.Width(22f)))
	                                    {
	                                        if (!deleteNames.Contains(textureName))
	                                        {
	                                            deleteNames.Add(textureName);
	                                        }
	                                    }
	                                }
								}
								else
								{
									if (deleteNames.Contains(textureName))
									{
										deleteNames.Remove(textureName);
									}
								}

                                GUILayout.Space(5f);
							}
							
							if (e.type == EventType.MouseUp)
							{
								var rect = GUILayoutUtility.GetLastRect();

								if (rect.Contains(e.mousePosition))
								{
									switch (e.button)
									{
										case 0:
											{
												selectionTextureName = textureName;
												if (textureInfos[i].texture != null)
												{
													EditorGUIUtility.PingObject(textureInfos[i].texture);
												}
												e.Use();
											}
											break;
									}
								}
							}
                        }

                        scrollPosition = scrollViewScope.scrollPosition;
                    }
                }
            }

			GUI.backgroundColor = defaultBackgroundColor;

			GUILayout.Space(2f);
		}

		private void DrawPatternTextureInfo(PatternTexture patternTexture)
		{
			if (patternTexture == null){ return; }

			EditorLayoutTools.Title("Info", EditorLayoutTools.BackgroundColor, EditorLayoutTools.LabelColor);

			GUILayout.Space(2f);

			if (patternTexture.Texture != null)
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					GUILayout.Label("Size :", GUILayout.Width(85f));
					GUILayout.Label($"{patternTexture.Texture.width}x{patternTexture.Texture.height}");
				}
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("Textures :", GUILayout.Width(85f));
				GUILayout.Label($"{patternTexture.GetAllPatternData().Count}");
			}

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.Label("Block Num :", GUILayout.Width(85f));
				GUILayout.Label($"{patternTexture.GetBlockCount()}");
			}

			EditorGUILayout.Separator();
		}

		private void DrawButtons(PatternTexture patternTexture)
		{
			var defaultBackgroundColor = GUI.backgroundColor;

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

			EditorGUILayout.Separator();
		}

		private void UpdateSelectPatternTextureInfo()
		{
			if(selectPatternTexture != null)
			{
				blockSize = selectPatternTexture.BlockSize;
				filterPixels = selectPatternTexture.FilterPixels;
				sizeType = selectPatternTexture.SizeType;

				Selection.objects = new UnityEngine.Object[0];

				BuildTextureInfos(null);
			}
			else
			{
				blockSize = DefaultBlockSize;
				filterPixels = DefaultFilterPixels;
				sizeType = PatternTexture.TextureSizeType.MultipleOf4;
			}

			deleteNames.Clear();
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

                var data = generator.Generate(exportPath, blockSize, BlockPadding, filterPixels, sizeType, textures, hasAlphaMap);

                patternTexture.Set(data.Texture, sizeType, blockSize, filterPixels, data.PatternData, data.PatternBlocks, hasAlphaMap);
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
