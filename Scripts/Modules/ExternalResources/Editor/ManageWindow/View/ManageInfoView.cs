
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.Project;

using Object = UnityEngine.Object;

namespace Modules.ExternalResource.Editor
{
    public sealed class ManageInfoView : LifetimeDisposable
    {
        //----- params -----

        private enum ViewMode
        {
            Contents,
            Detail,
        }

        //----- field -----

        private ViewMode viewMode = ViewMode.Contents;

        private IgnoreType? ignoreType = null;

        private Object manageAsset = null;
        private string manageAssetPath = null;
        private string displayManageAssetPath = null;

        private string contentDetailName = null;
        private Dictionary<string, AssetInfo[]> assetContents = null;

        private ContentsScrollView contentsScrollView = null;
        private ContentAssetsScrollView contentAssetsScrollView = null;

        private bool isShareAsset = false;

        private Subject<Unit> onUpdateManageInfo = null;
        private Subject<Unit> onDeleteManageInfo = null;

        private static GUIContent winbtnWinCloseIconContent = null;

        //----- property -----

        public bool IsOpen { get; private set; }

        public bool IsEdit { get; private set; }

        public ManageInfo ManageInfo { get; private set; }

        public IReadOnlyList<AssetInfo> ManagedAssetInfos { get; private set; }

        //----- method -----

        public ManageInfoView(AssetManagement assetManagement, ManageInfo manageInfo, string externalResourcesPath, string shareResourcesPath, IgnoreType? ignoreType, bool open, bool edit)
        {
            this.ignoreType = ignoreType;

            var externalResourcesDir = externalResourcesPath + PathUtility.PathSeparator;
            var shareResourcesDir = shareResourcesPath + PathUtility.PathSeparator;

            // 確定するまで元のインスタンスに影響を与えないようにコピーに対して編集を行う.
            ManageInfo = manageInfo.DeepCopy();

            IsOpen = open;
            IsEdit = edit;

            manageAssetPath = AssetDatabase.GUIDToAssetPath(manageInfo.guid);
            manageAsset = AssetDatabase.LoadMainAssetAtPath(manageAssetPath);

            displayManageAssetPath = string.Empty;

            if (manageAssetPath.StartsWith(externalResourcesDir))
            {
                isShareAsset = false;
                displayManageAssetPath = manageAssetPath.Substring(externalResourcesDir.Length, manageAssetPath.Length - externalResourcesDir.Length);
            }
            else if(manageAssetPath.StartsWith(shareResourcesDir))
            {
                isShareAsset = true;
                displayManageAssetPath = manageAssetPath.Substring(shareResourcesDir.Length, manageAssetPath.Length - shareResourcesDir.Length);
            }

            contentsScrollView = new ContentsScrollView();

            contentsScrollView.OnRequestDetailViewAsObservable()
                .Subscribe(x => SetDetailView(x))
                .AddTo(Disposable);

            contentAssetsScrollView = new ContentAssetsScrollView(externalResourcesPath, shareResourcesPath);
            
            BuildContentsInfo(assetManagement); 
        }

        public void Draw()
        {
            if (winbtnWinCloseIconContent == null)
            {
                winbtnWinCloseIconContent = EditorGUIUtility.IconContent("winbtn_win_close");
            }

            IsOpen = EditorLayoutTools.Header(displayManageAssetPath, IsOpen);

            if (IsOpen)
            {
                using (new ContentsScope())
                {
                    using (new DisableScope(!IsEdit))
                    {
                        var layoutWidth = 350f;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(5f);

                            using (new EditorGUILayout.VerticalScope())
                            {
                                GUILayout.Space(5f);

                                // このフィールドは編集不可.
                                EditorGUI.BeginDisabledGroup(true);
                                {
                                    EditorGUILayout.ObjectField(string.Empty, manageAsset, typeof(Object), false, GUILayout.Width(350f));
                                }
                                EditorGUI.EndDisabledGroup();
                            }

                            GUILayout.FlexibleSpace();

                            if (!IsEdit)
                            {
                                // Editボタンだけ常にアクティブ化させる.
                                EditorGUI.EndDisabledGroup();
                                {
                                    if (GUILayout.Button("Edit", GUILayout.Width(60f)))
                                    {
                                        IsEdit = true;
                                    }
                                }
                                EditorGUI.BeginDisabledGroup(!IsEdit);
                            }
                            else
                            {
                                if (GUILayout.Button("Delete", GUILayout.Width(60f)))
                                {
                                    if (EditorUtility.DisplayDialog("Confirm", "Remove naming rule.", "Apply", "Cancel"))
                                    {
                                        if (onDeleteManageInfo != null)
                                        {
                                            onDeleteManageInfo.OnNext(Unit.Default);
                                        }

                                        return;
                                    }
                                }

                                // 必要なパラメータが足りない時はApplyさせない.
                                var apply = true;

                                switch (ManageInfo.assetBundleNamingRule)
                                {
                                    case AssetBundleNamingRule.Specified:
                                    case AssetBundleNamingRule.PrefixAndChildAssetName:
                                        if (string.IsNullOrEmpty(ManageInfo.assetBundleNameStr))
                                        {
                                            apply = false;
                                        }
                                        break;
                                }

                                using (new DisableScope(!apply))
                                {
                                    if (GUILayout.Button("Apply"))
                                    {
                                        IsEdit = false;

                                        if (onUpdateManageInfo != null)
                                        {
                                            onUpdateManageInfo.OnNext(Unit.Default);
                                        }
                                    }
                                }
                            }

                            GUILayout.Space(5f);
                        }

                        GUILayout.Space(2f);

                        var originLabelWidth = EditorLayoutTools.SetLabelWidth(80f);

                        // 共有アセットはアセットバンドルのみ対応なので常にアセットバンドル.
                        if (!isShareAsset)
                        {
                            var ignoreAssetBundle = ignoreType.HasValue && ignoreType == IgnoreType.IgnoreAssetBundle;

                            if (ignoreAssetBundle)
                            {
                                if (ManageInfo.isAssetBundle)
                                {
                                    ManageInfo.isAssetBundle = false;
                                }
                            }
                            else
                            {
                                EditorGUI.BeginChangeCheck();

                                var isAssetBundle = EditorGUILayout.Toggle("AssetBundle", ManageInfo.isAssetBundle, GUILayout.Width(100f));

                                if (EditorGUI.EndChangeCheck())
                                {
                                    ManageInfo.isAssetBundle = isAssetBundle;
                                }

                                GUILayout.Space(2f);
                            }
                        }

                        if (ManageInfo.isAssetBundle)
                        {
                            EditorGUI.BeginChangeCheck();

                            var selectRuleTable = new AssetBundleNamingRule[]
                            {
                                AssetBundleNamingRule.ManageAssetName,
                                AssetBundleNamingRule.ChildAssetName,
                                AssetBundleNamingRule.PrefixAndChildAssetName,
								AssetBundleNamingRule.AssetFilePath,
                                AssetBundleNamingRule.Specified,
                            };

                            var labels = selectRuleTable.Select(x => x.ToString()).ToArray();

                            var index = selectRuleTable.IndexOf(x => x == ManageInfo.assetBundleNamingRule);

                            index = EditorGUILayout.Popup("Type", index, labels, GUILayout.Width(layoutWidth));

                            if (EditorGUI.EndChangeCheck())
                            {
                                ManageInfo.assetBundleNamingRule = index != -1 ? selectRuleTable[index] : AssetBundleNamingRule.None;
                                ManageInfo.assetBundleNameStr = null;
                            }

                            GUILayout.Space(2f);

                            var assetBundleNameStr = ManageInfo.assetBundleNameStr;

                            switch (ManageInfo.assetBundleNamingRule)
                            {
                                case AssetBundleNamingRule.Specified:
                                    assetBundleNameStr = EditorGUILayout.DelayedTextField("Specified", assetBundleNameStr, GUILayout.Width(layoutWidth));
                                    ManageInfo.assetBundleNameStr = assetBundleNameStr != null ? assetBundleNameStr.ToLower() : string.Empty;
                                    break;

                                case AssetBundleNamingRule.PrefixAndChildAssetName:
                                    assetBundleNameStr = EditorGUILayout.DelayedTextField("Prefix", assetBundleNameStr, GUILayout.Width(layoutWidth));
                                    ManageInfo.assetBundleNameStr = assetBundleNameStr != null ? assetBundleNameStr.ToLower() : string.Empty;
                                    break;
                            }
                        }

                        ManageInfo.tag = EditorGUILayout.DelayedTextField("Tag", ManageInfo.tag, GUILayout.Width(layoutWidth));

                        GUILayout.Space(2f);

                        ManageInfo.comment = EditorGUILayout.DelayedTextField("Memo", ManageInfo.comment, GUILayout.Width(layoutWidth), GUILayout.Height(38f));

                        EditorLayoutTools.SetLabelWidth(originLabelWidth);
                    }

                    EditorGUILayout.Separator();

                    switch (viewMode)
                    {
                        case ViewMode.Contents:
                            {
                                EditorLayoutTools.Title("Contents", new Color(0.7f, 0.9f, 0.7f));

                                using (new ContentsScope())
                                {
                                    var scrollEnable = 30 < contentsScrollView.Contents.Length;

                                    var options = scrollEnable ?
                                          new GUILayoutOption[] { GUILayout.Height(250) } :
                                          new GUILayoutOption[0];

                                    contentsScrollView.Draw(scrollEnable, options);
                                }
                            }
                            break;

                        case ViewMode.Detail:
                            {
                                using (new EditorGUILayout.HorizontalScope())
                                {
                                    EditorLayoutTools.Title(contentDetailName, new Color(0.3f, 0.3f, 1f));

                                    if (GUILayout.Button(winbtnWinCloseIconContent, GUILayout.Width(20f), GUILayout.Height(18f)))
                                    {
                                        SetContentsView();
                                    }
                                }

                                using (new ContentsScope())
                                {
                                    var scrollEnable = 30 < contentAssetsScrollView.Contents.Length;

                                    var options = scrollEnable ?
                                          new GUILayoutOption[] { GUILayout.Height(250) } :
                                          new GUILayoutOption[0];

                                    contentAssetsScrollView.Draw(scrollEnable, options);
                                }
                            }
                            break;
                    }
                }
            }
        }

        private void BuildContentsInfo(AssetManagement assetManagement)
        {
            assetContents = new Dictionary<string, AssetInfo[]>();

            var contents = new List<ContentsScrollView.Content>();

            var manageAssetPaths = assetManagement.GetManageAssetPaths(ManageInfo);

            ManagedAssetInfos = manageAssetPaths.Select(x => assetManagement.GetAssetInfo(x, ManageInfo)).ToArray();

            var assetBundleTargets = ManagedAssetInfos
                .Where(x => x.IsAssetBundle)
                .GroupBy(x => x.AssetBundle.AssetBundleName)
                .ToArray();
            
            foreach (var assetBundleTarget in assetBundleTargets)
            {
                var content = new ContentsScrollView.Content()
                {
                    label = assetBundleTarget.Key,
                    isAssetBundle = true,
                };

                contents.Add(content);

                assetContents.Add(assetBundleTarget.Key, assetBundleTarget.ToArray());
            }

            var otherAssetTargets = ManagedAssetInfos
                .Where(x => !x.IsAssetBundle)
                .ToArray();

            foreach (var otherAssetTarget in otherAssetTargets)
            {
                var assetContent = new ContentsScrollView.Content()
                {
                    label = otherAssetTarget.ResourcePath,
                    isAssetBundle = false,
                };

                contents.Add(assetContent);
            }

            contentsScrollView.Contents = contents.ToArray();
        }

        private void SetContentsView()
        {
            viewMode = ViewMode.Contents;

            contentDetailName = null;
            contentAssetsScrollView.Contents = null;
        }

        private void SetDetailView(string target)
        {
            viewMode = ViewMode.Detail;

            contentDetailName = target;

            var assets = assetContents.GetValueOrDefault(target);

            contentAssetsScrollView.Contents = assets;            
        }

        public IObservable<Unit> OnUpdateManageInfoAsObservable()
        {
            return onUpdateManageInfo ?? (onUpdateManageInfo = new Subject<Unit>());
        }

        public IObservable<Unit> OnDeleteManageInfoAsObservable()
        {
            return onDeleteManageInfo ?? (onDeleteManageInfo = new Subject<Unit>());
        }
    }

    public sealed class ContentsScrollView : EditorGUIFastScrollView<ContentsScrollView.Content>
    {
        public sealed class Content
        {
            public string label = null;
            public bool isAssetBundle = false;
        }

        private static GUIContent tabNextIconContent = null;

        private Subject<string> onRequestDetailView = null;

        public override Direction Type { get { return Direction.Vertical; } }

        protected override void DrawContent(int index, Content content)
        {
            if (tabNextIconContent == null)
            {
                tabNextIconContent = EditorGUIUtility.IconContent("tab_next");
            }

            GUILayout.Space(2f);

            using (new EditorGUILayout.HorizontalScope())
            {
                var type = content.isAssetBundle ? "AssetBundle" : "Other Assets";
                var color = content.isAssetBundle ? new Color(0.3f, 0.3f, 1f) : new Color(0.3f, 1f, 0.3f);

                var originLabelWidth = EditorLayoutTools.SetLabelWidth(75f);

                var titleStyle = new EditorLayoutTools.TitleGUIStyle
                {
                    backgroundColor = color,
                    width = 70f,
                };

                EditorLayoutTools.Title(type, titleStyle, GUILayout.Height(14f));

                EditorLayoutTools.SetLabelWidth(content.label);

                EditorGUILayout.SelectableLabel(content.label, GUILayout.Height(18f));

                EditorLayoutTools.SetLabelWidth(originLabelWidth);

                GUILayout.FlexibleSpace();

                if (content.isAssetBundle)
                {
                    if (GUILayout.Button(tabNextIconContent, EditorStyles.label, GUILayout.Width(20f), GUILayout.Height(20f)))
                    {
                        if (onRequestDetailView != null)
                        {
                            onRequestDetailView.OnNext(content.label);
                        }
                    }
                }
            }
        }

        public IObservable<string> OnRequestDetailViewAsObservable()
        {
            return onRequestDetailView ?? (onRequestDetailView = new Subject<string>());
        }
    }

    public sealed class ContentAssetsScrollView : EditorGUIFastScrollView<AssetInfo>
    {
        private string externalResourcesPath = null;
        private string shareResourcesPath = null;

        private Object[] assets = null;
        
        public override Direction Type { get { return Direction.Vertical; } }

        public ContentAssetsScrollView(string externalResourcesPath, string shareResourcesPath)
        {
            this.externalResourcesPath = externalResourcesPath;
            this.shareResourcesPath = shareResourcesPath;
        }

        protected override void OnContentsUpdate()
        {
            Func<AssetInfo, Object> load_asset = info =>
            {
                var assetPath = ExternalResources.GetAssetPathFromAssetInfo(externalResourcesPath, shareResourcesPath, info);

                var asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

                return asset;
            };

            assets = Contents.Select(x => load_asset(x)).ToArray();
        }

        protected override void DrawContent(int index, AssetInfo content)
        {
            EditorGUILayout.ObjectField(assets[index], typeof(Object), false);
        }
    }
}
