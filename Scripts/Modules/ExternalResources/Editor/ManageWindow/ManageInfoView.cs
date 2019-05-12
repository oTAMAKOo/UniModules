
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
    public class ManageInfoView
    {
        //----- params -----

        //----- field -----

        private ManageInfo manageInfo = null;
        private IgnoreType? ignoreType = null;
        private AssetCollectInfo[] assetInfos = null;
        private string manageAssetPath = null;

        private ContentsScrollView contentsScrollView = null;
        private bool scrollEnable = false;

        private Subject<Unit> onUpdateManageInfo = null;
        private Subject<Unit> onDeleteManageInfo = null;

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

            SetScrollViewContents(); 
        }

        public void Draw()
        {
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

                    EditorLayoutTools.DrawLabelWithBackground("Contents", new Color(0.7f, 0.9f, 0.7f));

                    using (new ContentsScope())
                    {
                        var options = scrollEnable ?
                            new GUILayoutOption[] { GUILayout.Height(250) } :
                            new GUILayoutOption[0];

                        contentsScrollView.Draw(scrollEnable, options);
                    }
                }
            }
        }

        private void SetScrollViewContents()
        {
            var contents = new List<ContentsScrollView.IScrollContent>();

            var assetBundleTargets = assetInfos
                .Where(x => x.AssetInfo.IsAssetBundle)
                .GroupBy(x => x.AssetInfo.AssetBundle.AssetBundleName)
                .ToArray();

            if (assetBundleTargets.Any())
            {
                foreach (var assetBundleTarget in assetBundleTargets)
                {
                    var title = string.Format("AssetBundle : {0}", assetBundleTarget.Key);
                    var headerContent = new ContentsScrollView.HeaderContent(title, new Color(0.3f, 0.3f, 1f));

                    contents.Add(headerContent);

                    foreach (var info in assetBundleTarget)
                    {
                        var assetContent = new ContentsScrollView.AssetContent(info.AssetPath, info.AssetInfo.ResourcesPath);

                        contents.Add(assetContent);
                    }
                }
            }

            var otherAssetTargets = assetInfos
                .Where(x => !x.AssetInfo.IsAssetBundle)               
                .ToArray();

            if (otherAssetTargets.Any())
            {
                var headerContent = new ContentsScrollView.HeaderContent("Other Assets", new Color(0.3f, 1f, 0.3f));

                contents.Add(headerContent);

                foreach (var otherAssetTarget in otherAssetTargets)
                {
                    var assetContent = new ContentsScrollView.AssetContent(otherAssetTarget.AssetPath, otherAssetTarget.AssetInfo.ResourcesPath);

                    contents.Add(assetContent);
                }
            }

            scrollEnable = 30 < contents.Count;
            contentsScrollView.Contents = contents.ToArray();
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

    public class ContentsScrollView : EditorGUIFastScrollView<ContentsScrollView.IScrollContent>
    {
        //----- params -----

        public interface IScrollContent
        {
            void Draw();
        }

        public class HeaderContent : IScrollContent
        {
            private string title = null;
            private Color color = Color.clear;

            public HeaderContent(string title, Color color)
            {
                this.title = title;
                this.color = color;
            }

            public void Draw()
            {
                EditorLayoutTools.DrawLabelWithBackground(title, color);
            }
        }

        public class AssetContent : IScrollContent
        {
            private string assetPath = null;
            private string assetLoadPath = null;
            private Vector2 assetLoadPathSize = Vector2.zero;

            public AssetContent(string assetPath, string assetLoadPath)
            {
                this.assetPath = assetPath;
                this.assetLoadPath = assetLoadPath;

                var textStyle = new GUIStyle();
                assetLoadPathSize = textStyle.CalcSize(new GUIContent(assetLoadPath));
            }

            public void Draw()
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("select", GUILayout.Width(55f), GUILayout.Height(20f)))
                    {
                        Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(assetPath);
                    }

                    GUILayout.Space(10f);

                    var originLabelWidth = EditorLayoutTools.SetLabelWidth(assetLoadPathSize.x);

                    EditorGUILayout.SelectableLabel(assetLoadPath, GUILayout.Height(20f));

                    EditorLayoutTools.SetLabelWidth(originLabelWidth);

                    GUILayout.FlexibleSpace();
                }
            }
        }

        //----- field -----

        //----- property -----

        public override Direction Type { get { return Direction.Vertical; } }

        //----- method -----

        protected override void DrawContent(int index, IScrollContent content)
        {
            content.Draw();
        }
    }
}
