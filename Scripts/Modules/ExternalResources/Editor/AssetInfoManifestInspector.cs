
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.ExternalResource
{
    [CustomEditor(typeof(AssetInfoManifest))]
    public sealed class AssetInfoManifestInspector : UnityEditor.Editor
    {
        //----- params -----

        private sealed class AssetCategoryInfo
        {
            public string Category { get; private set; }
            public AssetInfo[] AssetInfos { get; private set; }
            public AssetInfo[] SearchedInfos { get; private set; }
            public HashSet<int> OpenedIds { get; private set; }

            public AssetCategoryInfo(string category, AssetInfo[] assetInfos)
            {
                Category = category;
                AssetInfos = assetInfos;

                OpenedIds = new HashSet<int>();
            }

            public bool IsSearchedHit(string[] keywords)
            {
                var isHit = false;

                // カテゴリーが一致.
                isHit |= Category.IsMatch(keywords);

                // アセット情報が一致.
                SearchedInfos = AssetInfos.Where(x => IsAssetInfoSearchedHit(x, keywords)).ToArray();

                isHit |= SearchedInfos.Any();

                OpenedIds.Clear();

                return isHit;
            }

            public void ResetSearch()
            {
                SearchedInfos = null;
                OpenedIds.Clear();
            }

            private bool IsAssetInfoSearchedHit(AssetInfo assetInfo, string[] keywords)
            {
                var isHit = false;

                // ラベルが一致.
                if (assetInfo.Labels.Any())
                {
                    isHit |= assetInfo.Labels.Any(x => x.IsMatch(keywords));
                }

                // アセットバンドル名が一致.
                if (assetInfo.IsAssetBundle)
                {
                    isHit |= assetInfo.AssetBundle.AssetBundleName.IsMatch(keywords);
                }

                // 管理下のアセットのパスが一致.
                isHit |= assetInfo.ResourcePath.IsMatch(keywords);

                // ファイル名が一致.
                isHit |= assetInfo.FileName.IsMatch(keywords);

                return isHit;
            }
        }

        private sealed class AsstInfoScrollView : EditorGUIFastScrollView<AssetCategoryInfo>
        {
            private HashSet<int> openedIds = null;
            private GUIStyle textAreaStyle = null;

            public AsstInfoScrollView()
            {
                openedIds = new HashSet<int>();
            }

            public override Direction Type
            {
                get { return Direction.Vertical; }
            }

            protected override void DrawContent(int index, AssetCategoryInfo content)
            {
                if (textAreaStyle == null)
                {
                    textAreaStyle = GUI.skin.GetStyle("TextArea");
                    textAreaStyle.alignment = TextAnchor.MiddleLeft;
                    textAreaStyle.wordWrap = false;
                    textAreaStyle.stretchWidth = true;
                }

                var opened = openedIds.Contains(index);

                using (new EditorGUILayout.VerticalScope())
                {
                    var open = EditorLayoutTools.Header(content.Category, opened);

                    if (open)
                    {
                        using (new ContentsScope())
                        {
                            DrawGroupContent(content);
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

            private void DrawGroupContent(AssetCategoryInfo assetGroupInfo)
            {
                var assetInfos = assetGroupInfo.SearchedInfos ?? assetGroupInfo.AssetInfos;

                for (var i = 0; i < assetInfos.Length; i++)
                {
                    var opened = assetGroupInfo.OpenedIds.Contains(i);

                    var assetInfo = assetInfos[i];

                    var isAssetBundle = assetInfo.IsAssetBundle;

                    var color = isAssetBundle ? new Color(0.7f, 0.7f, 1f) : new Color(0.7f, 1f, 0.7f);

                    var open = EditorLayoutTools.Header(assetInfo.ResourcePath, opened, color);

                    if (open)
                    {
						var labels = string.Join(", ", assetInfo.Labels);

                        using (new ContentsScope())
                        {
                            EditorGUILayout.LabelField("ResourcesPath");
                            EditorGUILayout.SelectableLabel(assetInfo.ResourcePath, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("Category");
                            EditorGUILayout.SelectableLabel(assetInfo.Category, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("Label");
                            EditorGUILayout.SelectableLabel(labels, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("FileName");
                            EditorGUILayout.SelectableLabel(assetInfo.FileName, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("CRC");
                            EditorGUILayout.SelectableLabel(assetInfo.CRC, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("Hash");
                            EditorGUILayout.SelectableLabel(assetInfo.Hash, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("Size");
                            EditorGUILayout.SelectableLabel(assetInfo.Size.ToString(), textAreaStyle, GUILayout.Height(18f));

                            if (isAssetBundle)
                            {
                                EditorLayoutTools.ContentTitle("AssetBundle");

                                using (new ContentsScope())
                                {
                                    var assetBundle = assetInfo.AssetBundle;

                                    EditorGUILayout.LabelField("AssetBundleName");
                                    EditorGUILayout.SelectableLabel(assetBundle.AssetBundleName, textAreaStyle, GUILayout.Height(18f));

                                    if (assetBundle.Dependencies.Any())
                                    {
                                        EditorGUILayout.LabelField("Dependencies");
                                        foreach (var dependencie in assetBundle.Dependencies)
                                        {
                                            EditorGUILayout.SelectableLabel(dependencie, textAreaStyle, GUILayout.Height(18f));
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (!opened && open)
                    {
                        assetGroupInfo.OpenedIds.Add(i);
                    }

                    if (opened && !open)
                    {
                        assetGroupInfo.OpenedIds.Remove(i);
                    }
                }
            }
        }

        //----- field -----
        
        private AssetCategoryInfo[] currentInfos = null;
        private AssetCategoryInfo[] searchedInfos = null;
        private AsstInfoScrollView asstInfoScrollView = null;
        private string totalAssetCountText = null;
        private string searchText = null;
        private GUIStyle hashGuiStyle = null;

        [NonSerialized]
        private bool initialized = false;

        private AssetInfoManifest instance = null;

        //----- property -----

        //----- method -----

        public override void OnInspectorGUI()
        {
            instance = target as AssetInfoManifest;

            if (!initialized)
            {
                var assetInfos = Reflection.GetPrivateField<AssetInfoManifest, AssetInfo[]>(instance, "assetInfos");

                currentInfos = assetInfos.GroupBy(x => x.Category)
                    .Select(x => new AssetCategoryInfo(x.Key, x.ToArray()))
                    .OrderBy(x => x.Category, new NaturalComparer())
                    .ToArray();

                asstInfoScrollView = new AsstInfoScrollView();
                asstInfoScrollView.Contents = currentInfos;

                hashGuiStyle = new GUIStyle(EditorStyles.miniBoldLabel)
                {
                    alignment = TextAnchor.MiddleLeft,
                };

                totalAssetCountText = string.Format("Total Asset Count : {0}", assetInfos.Length);

                initialized = true;
            }

            DrawInspector();
        }

        private void DrawInspector()
        {
            if (!string.IsNullOrEmpty(instance.VersionHash))
            {
                EditorGUILayout.LabelField("Version:", GUILayout.Width(50f));

                GUILayout.Space(-5f);

                EditorGUILayout.SelectableLabel(instance.VersionHash, hashGuiStyle, GUILayout.Height(14f));
            }

            EditorGUILayout.LabelField(totalAssetCountText, GUILayout.Width(150f));

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                Action<string> onChangeSearchText = x =>
                {
                    searchText = x;

                    var searchKeywords = BuildSearchKeywords();

                    searchedInfos = currentInfos.Where(info => info.IsSearchedHit(searchKeywords)).ToArray();
                    asstInfoScrollView.ScrollPosition = Vector2.zero;
                    asstInfoScrollView.Contents = searchedInfos;

                    Repaint();
                };

                Action onSearchCancel = () =>
                {
                    searchText = string.Empty;
                    searchedInfos = null;

                    currentInfos.ForEach(x => x.ResetSearch());

                    asstInfoScrollView.ScrollPosition = Vector2.zero;
                    asstInfoScrollView.Contents = currentInfos;

                    Repaint();
                };

                EditorLayoutTools.DrawSearchTextField(searchText, onChangeSearchText, onSearchCancel);
            }

            GUILayout.Space(3f);

            asstInfoScrollView.Draw();
        }

        private string[] BuildSearchKeywords()
        {
            if (string.IsNullOrEmpty(searchText)) { return null; }

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            return keywords;
        }
    }
}
