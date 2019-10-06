
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.ExternalResource.Editor
{
    [CustomEditor(typeof(AssetInfoManifest))]
    public class AssetInfoManifestInspector : UnityEditor.Editor
    {
        //----- params -----

        private class AssetGroupInfo
        {
            public string GroupName { get; private set; }
            public AssetInfo[] AssetInfos { get; private set; }
            public AssetInfo[] SearchedInfos { get; private set; }
            public HashSet<int> OpenedIds { get; private set; }

            public AssetGroupInfo(string groupName, AssetInfo[] assetInfos)
            {
                GroupName = groupName;
                AssetInfos = assetInfos;

                OpenedIds = new HashSet<int>();
            }

            public bool IsSearchedHit(string[] keywords)
            {
                var isHit = false;

                // グループ名が一致.
                isHit |= GroupName.IsMatch(keywords);

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

                // タグが一致.
                if (!string.IsNullOrEmpty(assetInfo.Tag))
                {
                    isHit |= assetInfo.Tag.IsMatch(keywords);
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

        private class AsstInfoScrollView : EditorGUIFastScrollView<AssetGroupInfo>
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

            protected override void DrawContent(int index, AssetGroupInfo content)
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
                    var open = EditorLayoutTools.DrawHeader(content.GroupName, opened);

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

            private void DrawGroupContent(AssetGroupInfo assetGroupInfo)
            {
                var assetInfos = assetGroupInfo.SearchedInfos ?? assetGroupInfo.AssetInfos;

                for (var i = 0; i < assetInfos.Length; i++)
                {
                    var opened = assetGroupInfo.OpenedIds.Contains(i);

                    var assetInfo = assetInfos[i];

                    var isAssetBundle = assetInfo.IsAssetBundle;

                    var color = isAssetBundle ? new Color(0.7f, 0.7f, 1f) : new Color(0.7f, 1f, 0.7f);

                    var open = EditorLayoutTools.DrawHeader(assetInfo.ResourcePath, opened, color);

                    if (open)
                    {
                        using (new ContentsScope())
                        {
                            EditorGUILayout.LabelField("ResourcesPath");
                            EditorGUILayout.SelectableLabel(assetInfo.ResourcePath, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("GroupName");
                            EditorGUILayout.SelectableLabel(assetInfo.GroupName, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("Tag");
                            EditorGUILayout.SelectableLabel(assetInfo.Tag, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("FileName");
                            EditorGUILayout.SelectableLabel(assetInfo.FileName, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("FileHash");
                            EditorGUILayout.SelectableLabel(assetInfo.FileHash, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("FileSize");
                            EditorGUILayout.SelectableLabel(assetInfo.FileSize.ToString(), textAreaStyle, GUILayout.Height(18f));

                            if (isAssetBundle)
                            {
                                EditorLayoutTools.DrawContentTitle("AssetBundle");

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
        
        private AssetGroupInfo[] currentInfos = null;
        private AssetGroupInfo[] searchedInfos = null;
        private AsstInfoScrollView asstInfoScrollView = null;
        private string totalAssetCountText = null;
        private string searchText = null;

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

                currentInfos = assetInfos.GroupBy(x => x.GroupName)
                    .Select(x => new AssetGroupInfo(x.Key, x.ToArray()))
                    .ToArray();

                asstInfoScrollView = new AsstInfoScrollView();
                asstInfoScrollView.Contents = currentInfos;

                totalAssetCountText = string.Format("Total Asset Count : {0}", assetInfos.Length);

                initialized = true;
            }

            DrawInspector();
        }

        private void DrawInspector()
        {
            EditorGUILayout.LabelField(totalAssetCountText);

            EditorGUILayout.Separator();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();

                searchText = GUILayout.TextField(searchText, "SearchTextField", GUILayout.Width(250));

                if (EditorGUI.EndChangeCheck())
                {
                    var searchKeywords = BuildSearchKeywords();

                    searchedInfos = currentInfos.Where(x => x.IsSearchedHit(searchKeywords)).ToArray();
                    asstInfoScrollView.ScrollPosition = Vector2.zero;
                    asstInfoScrollView.Contents = searchedInfos;

                    Repaint();
                }

                if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                {
                    searchText = string.Empty;
                    searchedInfos = null;

                    currentInfos.ForEach(x => x.ResetSearch());

                    asstInfoScrollView.ScrollPosition = Vector2.zero;
                    asstInfoScrollView.Contents = currentInfos;

                    Repaint();
                }
            }

            EditorGUILayout.Separator();

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
