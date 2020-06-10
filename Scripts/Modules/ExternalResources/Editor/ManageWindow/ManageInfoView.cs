
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

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
        private ManageInfo manageInfo = null;
        private IgnoreType? ignoreType = null;
        private AssetCollectInfo[] assetInfos = null;
        private string manageAssetPath = null;

        private string contentDetailName = null;
        private Dictionary<string, AssetCollectInfo[]> assetContents = null;

        private ContentsScrollView contentsScrollView = null;
        private ContentAssetsScrollView contentAssetsScrollView = null;

        private Subject<Unit> onUpdateManageInfo = null;
        private Subject<Unit> onDeleteManageInfo = null;

        private static GUIContent winbtnWinCloseIconContent = null;

        //----- property -----

        public bool IsOpen { get; private set; }

        public bool IsEdit { get; private set; }

        public ManageInfo ManageInfo { get { return manageInfo; } }

        public AssetCollectInfo[] Infos { get { return assetInfos; } }

        //----- method -----

        public ManageInfoView(ManageInfo manageInfo, AssetCollectInfo[] assetInfos, IgnoreType? ignoreType, bool open, bool edit)
        {
            this.ignoreType = ignoreType;
            this.assetInfos = assetInfos;

            // 確定するまで元のインスタンスに影響を与えないようにコピーに対して編集を行う.
            this.manageInfo = new ManageInfo(manageInfo);

            IsOpen = open;
            IsEdit = edit;
            
            manageAssetPath = AssetDatabase.GetAssetPath(manageInfo.assetObject);

            contentsScrollView = new ContentsScrollView();

            contentsScrollView.OnRequestDetailViewAsObservable()
                .Subscribe(x => SetDetailView(x))
                .AddTo(Disposable);

            contentAssetsScrollView = new ContentAssetsScrollView();

            BuildContentsInfo(); 
        }

        public void Draw()
        {
            if (winbtnWinCloseIconContent == null)
            {
                winbtnWinCloseIconContent = EditorGUIUtility.IconContent("winbtn_win_close");
            }

            IsOpen = EditorLayoutTools.DrawHeader(manageAssetPath, IsOpen);

            if (IsOpen)
            {
                using (new ContentsScope())
                {
                    EditorGUI.BeginDisabledGroup(!IsEdit);
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
                                    EditorGUILayout.ObjectField(string.Empty, manageInfo.assetObject, typeof(Object), false, GUILayout.Width(350f));
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
                                    if (EditorUtility.DisplayDialog("確認", "命名規則を削除します", "続行", "中止"))
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

                                switch (manageInfo.assetBundleNameType)
                                {
                                    case ManageInfo.NameType.Specified:
                                    case ManageInfo.NameType.PrefixAndChildAssetName:
                                        if (string.IsNullOrEmpty(manageInfo.assetBundleNameStr))
                                        {
                                            apply = false;
                                        }
                                        break;
                                }

                                EditorGUI.BeginDisabledGroup(!apply);
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
                                EditorGUI.EndDisabledGroup();
                            }

                            GUILayout.Space(5f);
                        }

                        GUILayout.Space(2f);

                        var originLabelWidth = EditorLayoutTools.SetLabelWidth(80f);

                        if (!ignoreType.HasValue || ignoreType != IgnoreType.IgnoreAssetBundle)
                        {
                            EditorGUI.BeginChangeCheck();

                            var isAssetBundle = EditorGUILayout.Toggle("AssetBundle", manageInfo.isAssetBundle, GUILayout.Width(100f));

                            if (EditorGUI.EndChangeCheck())
                            {
                                manageInfo.isAssetBundle = isAssetBundle;
                            }
                        }

                        GUILayout.Space(2f);

                        if (manageInfo.isAssetBundle)
                        {
                            EditorGUI.BeginChangeCheck();

                            var assetBundleNameType = manageInfo.assetBundleNameType;

                            assetBundleNameType = (ManageInfo.NameType)EditorGUILayout.EnumPopup("Type", assetBundleNameType, GUILayout.Width(layoutWidth));

                            if (EditorGUI.EndChangeCheck())
                            {
                                manageInfo.assetBundleNameType = assetBundleNameType;
                                manageInfo.assetBundleNameStr = null;
                            }

                            GUILayout.Space(2f);

                            var assetBundleNameStr = manageInfo.assetBundleNameStr;

                            switch (manageInfo.assetBundleNameType)
                            {
                                case ManageInfo.NameType.Specified:
                                    assetBundleNameStr = EditorGUILayout.DelayedTextField("Specified", assetBundleNameStr, GUILayout.Width(layoutWidth));
                                    manageInfo.assetBundleNameStr = assetBundleNameStr != null ? assetBundleNameStr.ToLower() : string.Empty;
                                    break;

                                case ManageInfo.NameType.PrefixAndChildAssetName:
                                    assetBundleNameStr = EditorGUILayout.DelayedTextField("Prefix", assetBundleNameStr, GUILayout.Width(layoutWidth));
                                    manageInfo.assetBundleNameStr = assetBundleNameStr != null ? assetBundleNameStr.ToLower() : string.Empty;
                                    break;
                            }
                        }

                        manageInfo.tag = EditorGUILayout.DelayedTextField("Tag", manageInfo.tag, GUILayout.Width(layoutWidth));

                        GUILayout.Space(2f);

                        manageInfo.comment = EditorGUILayout.DelayedTextField("Memo", manageInfo.comment, GUILayout.Width(layoutWidth), GUILayout.Height(38f));

                        EditorLayoutTools.SetLabelWidth(originLabelWidth);
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.Separator();

                    switch (viewMode)
                    {
                        case ViewMode.Contents:
                            {
                                EditorLayoutTools.DrawLabelWithBackground("Contents", new Color(0.7f, 0.9f, 0.7f));

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
                                    EditorLayoutTools.DrawLabelWithBackground(contentDetailName, new Color(0.3f, 0.3f, 1f));

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

        private void BuildContentsInfo()
        {
            assetContents = new Dictionary<string, AssetCollectInfo[]>();

            var contents = new List<ContentsScrollView.Content>();

            var assetBundleTargets = assetInfos
                .Where(x => x.AssetInfo.IsAssetBundle)
                .GroupBy(x => x.AssetInfo.AssetBundle.AssetBundleName)
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

            var otherAssetTargets = assetInfos
                .Where(x => !x.AssetInfo.IsAssetBundle)
                .ToArray();

            foreach (var otherAssetTarget in otherAssetTargets)
            {
                var assetContent = new ContentsScrollView.Content()
                {
                    label = otherAssetTarget.AssetInfo.ResourcePath,
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

            using (new EditorGUILayout.HorizontalScope())
            {
                var type = content.isAssetBundle ? "AssetBundle" : "Other Assets";
                var color = content.isAssetBundle ? new Color(0.3f, 0.3f, 1f) : new Color(0.3f, 1f, 0.3f);

                var originLabelWidth = EditorLayoutTools.SetLabelWidth(75f);

                EditorLayoutTools.DrawLabelWithBackground(type, color, width: 70f, options: GUILayout.Height(15f));

                EditorLayoutTools.SetLabelWidth(content.label);

                EditorGUILayout.SelectableLabel(content.label, GUILayout.Height(18f));

                EditorLayoutTools.SetLabelWidth(originLabelWidth);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(tabNextIconContent, EditorStyles.label, GUILayout.Width(20f), GUILayout.Height(20f)))
                {
                    if (onRequestDetailView != null)
                    {
                        onRequestDetailView.OnNext(content.label);
                    }
                }
            }
        }

        public IObservable<string> OnRequestDetailViewAsObservable()
        {
            return onRequestDetailView ?? (onRequestDetailView = new Subject<string>());
        }
    }

    public sealed class ContentAssetsScrollView : EditorGUIFastScrollView<AssetCollectInfo>
    {
        private Object[] assets = null;

        public override Direction Type { get { return Direction.Vertical; } }

        protected override void OnContentsUpdate()
        {
            assets = Contents.Select(x => AssetDatabase.LoadMainAssetAtPath(x.AssetPath)).ToArray();
        }

        protected override void DrawContent(int index, AssetCollectInfo content)
        {
            EditorGUILayout.ObjectField(assets[index], typeof(Object), false);
        }
    }
}
