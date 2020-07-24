
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
    public sealed class ManageAssetView : LifetimeDisposable
    {
        //----- params -----

        private enum NameEditMode
        {
            None,
            New,
            Rename,
        }

        //----- field -----

        private AssetManageModel assetManageModel = null;
        private AssetManageManager assetManageManager = null;
        
        private GroupInfo selectionGroupInfo = null;
        private NameEditMode nameEditMode = NameEditMode.None;
        private string editGroupName = string.Empty;

        private ManageInfoView[] manageInfoviews = null;
        private ManageInfoView[] currentManageInfoviews = null;

        private string searchText = string.Empty;

        private Vector2 scrollPosition = Vector2.zero;

        private bool initialized = false;
        
        //----- property -----

        //----- method -----

        public void Initialize(AssetManageModel assetManageModel, AssetManageManager assetManageManager)
        {
            if(initialized) { return; }

            this.assetManageModel = assetManageModel;
            this.assetManageManager = assetManageManager;

            assetManageModel.OnDragAndDropAsObservable().Subscribe(x => OnDragAndDrop(x)).AddTo(Disposable);

            assetManageManager.CollectInfo();

            selectionGroupInfo = null;

            BuildManageInfoViews();

            initialized = true;
        }

        public void DrawHeaderGUI()
        {
            var groupManageInfos = assetManageManager.GroupInfos.ToArray();

            var groupNames = groupManageInfos.Select(x => x.groupName).ToArray();

            var selection = selectionGroupInfo != null ?
                groupManageInfos.IndexOf(x => x.groupName == selectionGroupInfo.groupName) :
                -1;

            if (nameEditMode != NameEditMode.None)
            {
                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();

                editGroupName = EditorGUILayout.DelayedTextField(editGroupName, GUILayout.Width(250f));

                if (EditorGUI.EndChangeCheck())
                {
                    if (!string.IsNullOrEmpty(editGroupName))
                    {
                        switch (nameEditMode)
                        {
                            case NameEditMode.New:
                                var groupManageInfo = new GroupInfo(editGroupName);
                                assetManageManager.AddGroupInfo(groupManageInfo);
                                selectionGroupInfo = groupManageInfo;
                                break;

                            case NameEditMode.Rename:

                                if (selectionGroupInfo.groupName != editGroupName)
                                {
                                    assetManageManager.RenameGroupInfo(selectionGroupInfo.groupName, editGroupName);

                                    selectionGroupInfo = assetManageManager.GroupInfos.FirstOrDefault(x => x.groupName == editGroupName);

                                    var groupAssetPaths = assetManageManager.GetGroupAssetPaths(selectionGroupInfo);

                                    UpdateAssetInfo(groupAssetPaths);
                                }
                                break;
                        }

                        BuildManageInfoViews();
                    }

                    nameEditMode = NameEditMode.None;

                    editGroupName = null;

                    assetManageModel.RequestRepaint();
                }
            }
            else
            {
                if (GUILayout.Button("追加", EditorStyles.toolbarButton))
                {
                    nameEditMode = NameEditMode.New;
                }

                if(selectionGroupInfo != null)
                {
                    if (GUILayout.Button("削除", EditorStyles.toolbarButton))
                    {
                        if (EditorUtility.DisplayDialog("確認", "グループを削除します", "実行", "中止"))
                        {
                            var assetPaths = assetManageManager.GetGroupAssetPaths(selectionGroupInfo);

                            assetManageManager.DeleteGroupInfo(selectionGroupInfo);

                            selectionGroupInfo = assetManageManager.GroupInfos.First();

                            UpdateAssetInfo(assetPaths);

                            BuildManageInfoViews();

                            assetManageModel.RequestRepaint();

                            return;
                        }
                    }

                    if (GUILayout.Button("リネーム", EditorStyles.toolbarButton))
                    {
                        nameEditMode = NameEditMode.Rename;

                        editGroupName = selectionGroupInfo.groupName;

                        assetManageModel.RequestRepaint();
                        
                        return;
                    }
                }

                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();

                var groupName = groupNames.ElementAtOrDefault(selection);

                var labels = groupNames.OrderBy(x => x, new NaturalComparer()).ToArray();

                var index = labels.IndexOf(x => x == groupName);
                
                var displayLabels = labels.Select(x => ConvertSlashToUnicodeSlash(x)).ToArray();

                index = EditorGUILayout.Popup(string.Empty, index, displayLabels, EditorStyles.toolbarDropDown, GUILayout.Width(180f));

                if (EditorGUI.EndChangeCheck())
                {
                    selection = groupNames.IndexOf(x => x == labels[index]);

                    selectionGroupInfo = groupManageInfos[selection];

                    BuildManageInfoViews();
                }
            }
        }

        private string ConvertSlashToUnicodeSlash(string text)
        {
            return text.Replace('/', '\u2215');
        }

        public void DrawGUI()
        {
            if(nameEditMode != NameEditMode.None)
            {
                EditorGUILayout.HelpBox("Can not operate while entering name.", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                EditorGUI.BeginChangeCheck();
                
                searchText = EditorGUILayout.TextField(string.Empty, searchText, "SearchTextField", GUILayout.Width(200f));

                if (EditorGUI.EndChangeCheck())
                {
                    scrollPosition = Vector2.zero;
                    UpdateSearchedEntrys();
                }

                if (GUILayout.Button(string.Empty, "SearchCancelButton", GUILayout.Width(18f)))
                {
                    searchText = string.Empty;
                    GUIUtility.keyboardControl = 0;
                    scrollPosition = Vector2.zero;

                    UpdateSearchedEntrys();
                }
            }

            if (currentManageInfoviews != null)
            {
                using (var scrollViewScope = new EditorGUILayout.ScrollViewScope(scrollPosition))
                {
                    foreach (var view in currentManageInfoviews)
                    {
                        view.Draw();

                        GUILayout.Space(5f);
                    }

                    scrollPosition = scrollViewScope.scrollPosition;
                }       
            }
        }

        private void UpdateAssetInfo(string[] assetPaths)
        {
            var refresh = false;

            AssetDatabase.StartAssetEditing();

            for (var i = 0; i < assetPaths.Length; i++)
            {
                var assetPath = assetPaths[i];

                EditorUtility.DisplayProgressBar("Update asset info", assetPath, (float)i / assetPaths.Length);

                // アセット情報収集.
                var infos = assetManageManager.CollectInfo(assetPath);

                foreach (var info in infos)
                {
                    // アセットバンドル名適用.
                    refresh |= info.ApplyAssetBundleName(assetManageManager);
                }
            }

            EditorUtility.ClearProgressBar();

            AssetDatabase.StopAssetEditing();

            if (refresh)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private ManageInfoView CreateManageInfoView(ManageInfo manageInfo, AssetCollectInfo[] collectInfos, bool opened, bool edited)
        {
            var manageAssetPath = AssetDatabase.GetAssetPath(manageInfo.assetObject);
            var ignoreType = assetManageManager.GetIgnoreType(manageAssetPath);
            var assetInfos = collectInfos
                .Where(x => x.AssetInfo != null)
                .Where(x => x.ManageInfo == manageInfo)
                .ToArray();

            var view = new ManageInfoView(manageInfo, assetInfos, ignoreType, opened, edited);
            
            view.OnUpdateManageInfoAsObservable()
                .DelayFrame(1)
                .Subscribe(_ =>
                    {
                        assetManageManager.UpdateManageInfo(view.ManageInfo);
                        UpdateAssetInfo(view.Infos.Select(x => x.AssetPath).ToArray());
                        BuildManageInfoViews();
                    })
                .AddTo(Disposable);

            view.OnDeleteManageInfoAsObservable()
                .DelayFrame(1)
                .Subscribe(_ =>
                    {
                        assetManageManager.DeleteManageInfo(selectionGroupInfo.groupName, view.ManageInfo);
                        UpdateAssetInfo(view.Infos.Select(x => x.AssetPath).ToArray());
                        BuildManageInfoViews();
                    })
                .AddTo(Disposable);

            return view;
        }

        private void BuildManageInfoViews()
        {
            if(selectionGroupInfo == null) { return; }

            // 更新前の状態を保持.

            var opened = new Object[0];

            if (manageInfoviews != null)
            {
                opened = manageInfoviews
                    .Where(x => x.IsOpen)
                    .Select(x => x.ManageInfo.assetObject)
                    .ToArray();
            }

            var edited = new Object[0];

            if (manageInfoviews != null)
            {
                edited = manageInfoviews
                    .Where(x => x.IsEdit)
                    .Select(x => x.ManageInfo.assetObject)
                    .ToArray();
            }

            // グループ内の管理情報取得.

            var manageInfos = assetManageManager
                .GetGroupManageInfo(selectionGroupInfo.groupName)
                .ToArray();

            var views = new List<ManageInfoView>();

            var collectInfos = assetManageManager.GetAllAssetCollectInfo().ToArray();

            // グループ内で管理しているアセット管理情報.
            foreach (var manageInfo in manageInfos)
            {
                var manageAsset = manageInfo.assetObject;

                var open = opened.Any(x => x == manageAsset);
                var edit = edited.Any(x => x == manageAsset);

                var view = CreateManageInfoView(manageInfo, collectInfos, open, edit);

                views.Add(view);
            }

            manageInfoviews = views.ToArray();

            UpdateSearchedEntrys();
        }

        private void OnDragAndDrop(Object[] assetObjects)
        {
            if (selectionGroupInfo == null || string.IsNullOrEmpty(selectionGroupInfo.groupName)) { return; }

            var assetObject = assetObjects.FirstOrDefault();

            if (assetObject == null) { return; }

            if (!assetManageManager.ValidateManageInfo(assetObject)) { return; }

            var assetPath = AssetDatabase.GetAssetPath(assetObject);
            
            // 管理情報を追加.
            var manageInfo = assetManageManager.AddManageInfo(selectionGroupInfo.groupName, assetObject);

            if (manageInfo != null)
            {
                // 追加された子階層の情報を再収集.

                var progress = new ScheduledNotifier<float>();

                progress.Subscribe(
                        x =>
                        {
                            EditorUtility.DisplayProgressBar("Progress", "Collecting asset info", x);
                        })
                    .AddTo(Disposable);

                assetManageManager.CollectInfo(assetPath, progress);

                EditorUtility.ClearProgressBar();

                // 更新された情報.
                var collectInfos = assetManageManager.GetAllAssetCollectInfo().ToArray();

                // View追加.
                var view = CreateManageInfoView(manageInfo, collectInfos, true, true);

                manageInfoviews = manageInfoviews.Concat(new ManageInfoView[] { view  }).ToArray();

                // 管理下に入ったアセットの情報を更新.

                var assetPaths = view.Infos.Select(x => x.AssetPath).ToArray();

                UpdateAssetInfo(assetPaths);

                // View再構築.
                BuildManageInfoViews();
            }
        }

        private void UpdateSearchedEntrys()
        {
            currentManageInfoviews = string.IsNullOrEmpty(searchText) ?
                manageInfoviews :
                manageInfoviews.Where(x => IsSearchedHit(x)).ToArray();
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
            isHit |= view.Infos
                 .Where(x => x.AssetInfo.IsAssetBundle)
                 .Any(x => x.AssetInfo.AssetBundle.AssetBundleName.IsMatch(keywords));

            // 管理下のアセットのパスが一致.
            isHit |= view.Infos.Any(x => x.AssetPath.IsMatch(keywords));
            
            return isHit;
        }
    }
}
