
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

        private class AsstInfoScrollView : EditorGUIFastScrollView<AssetInfo>
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

            protected override void DrawContent(int index, AssetInfo content)
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
                    var isAssetBundle = content.IsAssetBundle;

                    var color = isAssetBundle ? new Color(0.7f, 0.7f, 1f) : new Color(0.7f, 1f, 0.7f);

                    var open = EditorLayoutTools.DrawHeader(content.ResourcesPath, opened, color);

                    if (open)
                    {
                        using (new ContentsScope())
                        {
                            EditorGUILayout.LabelField("ResourcesPath");
                            EditorGUILayout.SelectableLabel(content.ResourcesPath, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("GroupName");
                            EditorGUILayout.SelectableLabel(content.GroupName, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("FileHash");
                            EditorGUILayout.SelectableLabel(content.FileHash, textAreaStyle, GUILayout.Height(18f));

                            EditorGUILayout.LabelField("FileSize");
                            EditorGUILayout.SelectableLabel(content.FileSize.ToString(), textAreaStyle, GUILayout.Height(18f));

                            if (isAssetBundle)
                            {
                                EditorLayoutTools.DrawContentTitle("AssetBundle");

                                using (new ContentsScope())
                                {
                                    var assetBundle = content.AssetBundle;

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

        private AssetInfo[] assetInfos = null;
        private AssetInfo[] currentAssetInfos = null;
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
                assetInfos = Reflection.GetPrivateField<AssetInfoManifest, AssetInfo[]>(instance, "assetInfos");

                currentAssetInfos = assetInfos;

                asstInfoScrollView = new AsstInfoScrollView();
                asstInfoScrollView.Contents = currentAssetInfos;

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
                    currentAssetInfos = assetInfos.Where(x => IsSearchedHit(x)).ToArray();
                    asstInfoScrollView.ScrollPosition = Vector2.zero;
                    asstInfoScrollView.Contents = currentAssetInfos;

                    Repaint();
                }

                if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                {
                    searchText = string.Empty;
                    currentAssetInfos = assetInfos;
                    asstInfoScrollView.ScrollPosition = Vector2.zero;
                    asstInfoScrollView.Contents = currentAssetInfos;

                    Repaint();
                }
            }

            EditorGUILayout.Separator();

            asstInfoScrollView.Draw();
        }

        private bool IsSearchedHit(AssetInfo info)
        {
            if (string.IsNullOrEmpty(searchText)) { return true; }

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            var isHit = false;

            // アセットバンドル名が一致.
            if (info.IsAssetBundle)
            {
                isHit |= info.AssetBundle.AssetBundleName.IsMatch(keywords);
            }

            // 管理下のアセットのパスが一致.
            isHit |= info.ResourcesPath.IsMatch(keywords);
            
            return isHit;
        }
    }
}
