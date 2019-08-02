
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Extensions.Devkit;

namespace Modules.Devkit.AssetTuning
{
    public sealed class CompressCheckWindow : SingletonEditorWindow<CompressCheckWindow>
    {
        //----- params -----

        private sealed class TextureAssetInfo
        {
            public Texture textureAsset;
            public string compressPath;
            public Vector2 size;
            public bool compress;
        }

        private sealed class CompressInfo
        {
            public string compressPath;
            public TextureAssetInfo[] assetInfos = null;
        }

        private sealed class TextureAssetInfoScrollView : EditorGUIFastScrollView<CompressInfo>
        {
            //----- params -----

            //----- field -----

            private HashSet<int> openedIds = null;
            private GUIStyle labelStyle = null;

            //----- property -----

            public override Direction Type { get { return Direction.Vertical; } }

            //----- method -----

            public TextureAssetInfoScrollView()
            {
                openedIds = new HashSet<int>();
            }

            protected override void DrawContent(int index, CompressInfo content)
            {
                if (labelStyle == null)
                {
                    labelStyle = GUI.skin.label;
                    labelStyle.alignment = TextAnchor.MiddleLeft;
                    labelStyle.wordWrap = false;
                    labelStyle.stretchWidth = false;
                }

                var opened = openedIds.Contains(index);

                using (new EditorGUILayout.VerticalScope())
                {
                    var open = EditorLayoutTools.DrawHeader(content.compressPath, opened);

                    if (open)
                    {
                        using (new ContentsScope())
                        {
                            using (new ContentsScope())
                            {
                                foreach (var item in content.assetInfos)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        if (item.compress)
                                        {
                                            EditorLayoutTools.DrawLabelWithBackground("Compress", Color.cyan, Color.white, TextAnchor.MiddleCenter, width: 50f);
                                        }
                                        else
                                        {
                                            EditorLayoutTools.DrawLabelWithBackground("Failed", Color.red, Color.white, TextAnchor.MiddleCenter, width: 85f);
                                        }

                                        EditorGUILayout.ObjectField(item.textureAsset, typeof(Texture), false);

                                        EditorGUILayout.TextField(string.Format("{0}x{1}", item.size.x, item.size.y), labelStyle, GUILayout.Width(85f));
                                    }
                                }
                            }
                        }
                    }

                    if (!opened && open)
                    {
                        openedIds.Add(index);
                    }

                    if (opened && !open)
                    {
                        openedIds.Remove(index);
                    }
                }
            }
        }

        //----- field -----

        private CompressInfo[] textureAssetInfos = null;
        private TextureAssetInfoScrollView scrollView = null;
        private bool failedOnly = false;

        private bool initialized = false;

        //----- property -----

        //----- method -----
        
        public static void Open()
        {
            Instance.Initialize();
        }

        private void Initialize()
        {
            if (initialized) { return; }

            titleContent = new GUIContent("CompressCheckWindow");

            scrollView = new TextureAssetInfoScrollView();
            
            initialized = true;

            Show();
        }

        void OnGUI()
        {
            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(5f);

                if (GUILayout.Button("Search Textures", GUILayout.Width(150f)))
                {
                    textureAssetInfos = BuildCompressInfo().ToArray();
                    scrollView.Contents = textureAssetInfos;
                    Repaint();
                }

                GUILayout.FlexibleSpace();

                var originLabelWidth = EditorLayoutTools.SetLabelWidth(70f);

                failedOnly = EditorGUILayout.Toggle("Failed Only", failedOnly, GUILayout.Width(90f));

                EditorLayoutTools.SetLabelWidth(originLabelWidth);

                GUILayout.Space(5f);
            }

            EditorGUILayout.Separator();

            if (textureAssetInfos == null)
            {
                EditorGUILayout.HelpBox("Press search texture.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.Separator();

                scrollView.Draw();
            }
        }

        private CompressInfo[] BuildCompressInfo()
        {
            var configs = TextureAssetTunerConfig.Instance;

            var infos = new List<TextureAssetInfo>();
            
            foreach (var target in configs.CompressFolders)
            {
                if (target == null) { continue; }

                var path = AssetDatabase.GetAssetPath(target);

                if (!AssetDatabase.IsValidFolder(path)) { continue; }
                
                var textures = UnityEditorUtility.FindAssetsByType<Texture2D>("t:texture", new string[] { path }).ToArray();

                var title = string.Format("Find : {0}", path);

                for (var i = 0; i < textures.Length; i++)
                {
                    var texture = textures[i];

                    var assetPath = AssetDatabase.GetAssetPath(texture);

                    EditorUtility.DisplayProgressBar(title, assetPath, (float)i / textures.Length);

                    var info = BuildTextureAssetInfo(path, texture);

                    if (failedOnly)
                    {
                        if (!info.compress)
                        {
                            infos.Add(info);
                        }
                    }
                    else
                    {
                        infos.Add(info);
                    }
                }

                EditorUtility.ClearProgressBar();
            }

            return infos.GroupBy(x => x.compressPath)
                .Select(x => new CompressInfo() { compressPath = x.Key, assetInfos = x.ToArray() })
                .Where(x => x.assetInfos.Any())
                .ToArray();
        }

        private TextureAssetInfo BuildTextureAssetInfo(string compressPath, Texture texture)
        {
            if (texture == null) { return null; }

            var size = new Vector2(texture.width, texture.height);

            var compress = IsMultipleOf4(size.x) && IsMultipleOf4(size.y);

            var info = new TextureAssetInfo()
            {
                textureAsset = texture,
                compressPath = compressPath,
                size = size,
                compress = compress,
            };

            return info;
        }

        private static bool IsMultipleOf4(float value)
        {
            return value % 4 == 0;
        }
    }
}
