
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Extensions.Devkit;

namespace Modules.ExternalAssets
{
    public sealed class ManageAssetView : LifetimeDisposable
    {
        //----- params -----

        //----- field -----

        private AssetManagement assetManagement = null;
        private string externalAssetPath = null;
        private string shareResourcesPath = null;

        private ManageInfoView[] manageInfoViews = null;
        private ManageInfoView[] currentManageInfoViews = null;

        private string group = null;

        private string searchText = string.Empty;

        private Vector2 scrollPosition = Vector2.zero;

        private Subject<Unit> onRequestRepaint = null;

        private bool initialized = false;

        //----- property -----

        //----- method -----

        public void Initialize(AssetManagement assetManagement, string externalAssetPath, string shareResourcesPath)
        {
            if (initialized) { return; }
            
            this.assetManagement = assetManagement;
            this.externalAssetPath = externalAssetPath;
            this.shareResourcesPath = shareResourcesPath;

            BuildManageInfoViews().Forget();

            initialized = true;
        }

        public void DrawGUI()
        {
			// 検索バー.

            void OnChangeSearchText(string x)
            {
                searchText = x;
                scrollPosition = Vector2.zero;
                UpdateSearchedViews();
            }

            void OnSearchCancel()
            {
                searchText = string.Empty;
                scrollPosition = Vector2.zero;
                UpdateSearchedViews();
            }

            EditorLayoutTools.DrawSearchTextField(searchText, OnChangeSearchText, OnSearchCancel);

			// 管理中の情報View.

			GUILayout.Space(4f);

            if (currentManageInfoViews != null)
            {
                using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                {
                    foreach (var view in currentManageInfoViews)
                    {
                        view.Draw();

                        GUILayout.Space(5f);
                    }

                    scrollPosition = scrollViewScope.scrollPosition;
                }
            }
        }

        public void SetGroup(string group)
        {
            this.group = group;

            BuildManageInfoViews().Forget();
        }

        private async UniTask UpdateAssetInfo(string[] targetAssetPaths)
        {
            var refresh = false;

            using (new AssetEditingScope())
            {
                for (var i = 0; i < targetAssetPaths.Length; i++)
                {
                    var targetAssetPath = targetAssetPaths[i];

                    // アセット情報収集.
                    var infos = await assetManagement.GetAssetInfos(targetAssetPath);

                    foreach (var info in infos)
                    {
                        var assetDir = info.Group == ExternalAsset.ShareGroupName ? shareResourcesPath : externalAssetPath;
                        var assetPath = PathUtility.Combine(assetDir, info.ResourcePath);

                        EditorUtility.DisplayProgressBar("Update asset info", info.ResourcePath, (float)i / targetAssetPaths.Length);

                        if (info.AssetBundle == null) { continue; }

                        var assetBundleName = info.AssetBundle.AssetBundleName;

                        // アセットバンドル名適用.
                        refresh |= assetManagement.SetAssetBundleName(assetPath, assetBundleName);
                    }
                }

                EditorUtility.ClearProgressBar();
            }

            if (refresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private async UniTask<ManageInfoView> CreateManageInfoView(ManageInfo manageInfo, bool opened, bool edited)
        {
            var manageAssetPath = AssetDatabase.GUIDToAssetPath(manageInfo.guid);
            var ignoreType = assetManagement.GetIgnoreType(manageAssetPath);
            
            var view = new ManageInfoView(manageInfo, externalAssetPath, shareResourcesPath, ignoreType, opened, edited);

            await view.BuildContentsInfo(assetManagement); 

            view.OnUpdateManageInfoAsObservable()
                .DelayFrame(1)
                .Subscribe(async _ =>
                    {
                        assetManagement.UpdateManageInfo(view.ManageInfo);

                        var updateAssetPaths = await assetManagement.GetManageAssetPaths(view.ManageInfo);

                        await UpdateAssetInfo(updateAssetPaths);

                        await BuildManageInfoViews();
                    })
                .AddTo(Disposable);

            view.OnDeleteManageInfoAsObservable()
                .DelayFrame(1)
                .Subscribe(async _ =>
                    {
                        var updateAssetPaths = await assetManagement.GetManageAssetPaths(view.ManageInfo);

                        assetManagement.DeleteManageInfo(view.ManageInfo);

                        await UpdateAssetInfo(updateAssetPaths);

                        await BuildManageInfoViews();
                    })
                .AddTo(Disposable);

            return view;
        }

        public async UniTask BuildManageInfoViews()
        {
            if (string.IsNullOrEmpty(group)) { return; }

            // 更新前の状態を保持.

            var opened = new string[0];

            if (manageInfoViews != null)
            {
                opened = manageInfoViews
                    .Where(x => x.IsOpen)
                    .Select(x => x.ManageInfo.guid)
                    .ToArray();
            }

            var edited = new string[0];

            if (manageInfoViews != null)
            {
                edited = manageInfoViews
                    .Where(x => x.IsEdit)
                    .Select(x => x.ManageInfo.guid)
                    .ToArray();
            }

            // グループ内の管理情報取得.

            var manageInfos = assetManagement.GetManageInfos(group);

            var views = new List<ManageInfoView>();
            
            // グループ内で管理しているアセット管理情報.
            foreach (var manageInfo in manageInfos)
            {
                var guid = manageInfo.guid;

                var open = opened.Any(x => x == guid);
                var edit = edited.Any(x => x == guid);

                var view = await CreateManageInfoView(manageInfo, open, edit);

                views.Add(view);
            }

            manageInfoViews = views.ToArray();

            UpdateSearchedViews();

            EditorApplication.delayCall += () =>
            {
                if (onRequestRepaint != null)
                {
                    onRequestRepaint.OnNext(Unit.Default);
                }
            };
        }

        private void UpdateSearchedViews()
        {
            currentManageInfoViews = string.IsNullOrEmpty(searchText) ?
                manageInfoViews :
                manageInfoViews.Where(x => IsSearchedHit(x)).ToArray();
        }

        private bool IsSearchedHit(ManageInfoView view)
        {
            if (string.IsNullOrEmpty(searchText)) { return true; }

            var keywords = searchText.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < keywords.Length; ++i)
            {
                keywords[i] = keywords[i].ToLower();
            }

            var isHit = false;

            // アセットバンドル名が一致.
            isHit |= view.ManagedAssetInfos
                 .Where(x => x.IsAssetBundle)
                 .Any(x => x.AssetBundle.AssetBundleName.IsMatch(keywords));

            // 管理下のアセットのパスが一致.
            isHit |= view.ManagedAssetInfos.Any(x => x.ResourcePath.IsMatch(keywords));

            return isHit;
        }

        public IObservable<Unit> OnRequestRepaintAsObservable()
        {
            return onRequestRepaint ?? (onRequestRepaint = new Subject<Unit>());
        }
    }
}
