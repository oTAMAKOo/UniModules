
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Inspector
{
    public sealed class FolderRegisterScrollView : RegisterScrollView<FolderRegisterScrollView.FolderInfo>
    {
        //----- params -----

        private enum AssetViewMode
        {
            Asset,
            Path
        }

        public class FolderInfo
        {
            public string assetPath = null;
            public Object asset = null;
        }

        //----- field -----

        private string title = null;
        private string headerKey = null;
        private AssetViewMode assetViewMode = AssetViewMode.Asset;
        
        private Subject<Object[]> onUpdateContents = null;

        //----- property -----

        /// <summary>
        /// 既に子階層のフォルダが登録済みの時そのフォルダを除外するか.
        /// </summary>
        public bool RemoveChildrenFolder { get; set; }

        //----- method -----

        public FolderRegisterScrollView(string title, string headerKey)
        {
            this.title = title;
            this.headerKey = headerKey;

            OnUpdateContentsAsObservable()
                .Subscribe(x =>
                    {
                        if (onUpdateContents != null)
                        {
                            var folders = x.Select(y => y.asset).ToArray();

                            onUpdateContents.OnNext(folders);
                        }
                    })
                .AddTo(Disposable);

            RemoveChildrenFolder = false;            
        }

        // 外部公開しない.
        private new void SetContents(FolderInfo[] contents) { }

        public void SetContents(Object[] folders)
        {
            Contents = folders
                .Select(x =>
                    {
                        var info = new FolderInfo()
                        {
                            asset = x,
                            assetPath = x != null ? AssetDatabase.GetAssetPath(x) : string.Empty,
                        };

                        return info;
                    })
                .ToArray();       
        }

        protected override FolderInfo DrawContent(int index, FolderInfo info)
        {
            switch (assetViewMode)
            {
                case AssetViewMode.Asset:
                    {
                        EditorGUI.BeginChangeCheck();

                        var folder = EditorGUILayout.ObjectField(info.asset, typeof(Object), false);

                        if (EditorGUI.EndChangeCheck())
                        {
                            if (CheckFolderAsset(folder))
                            {
                                info = new FolderInfo()
                                {
                                    asset = folder,
                                    assetPath = AssetDatabase.GetAssetPath(folder),
                                };

                                // 一旦ローカル配列に変換してから上書き.
                                var contents = Contents.ToArray();

                                contents[index] = info;

                                // 親が登録された場合子階層を除外.
                                if (RemoveChildrenFolder)
                                {
                                    if (CheckParentFolderRegisted(folder, Contents))
                                    {
                                        var removeChildrenInfos = GetRemoveChildrenFolders(folder, Contents);

                                        if (removeChildrenInfos.Any())
                                        {
                                            EditorUtility.DisplayDialog("Removed Folder", "Removed registration of child folders.", "Close");

                                            contents = contents.Where(x => !removeChildrenInfos.Contains(x)).ToArray();
                                        }
                                    }
                                }

                                Contents = contents;
                            }
                        }
                    }
                    break;

                case AssetViewMode.Path:
                    GUILayout.Label(info.assetPath, EditorLayoutTools.TextAreaStyle);
                    break;
            }

            return info;
        }

        protected override FolderInfo CreateNewContent()
        {
            var compressFolder = new FolderInfo()
            {
                asset = null,
                assetPath = string.Empty,
            };

            return compressFolder;
        }

        protected override void DrawHeaderContent()
        {
            GUILayout.Space(5f);

            EditorGUI.BeginChangeCheck();

            var mode = (AssetViewMode)EditorGUILayout.EnumPopup(assetViewMode, GUILayout.Width(60f));

            if (EditorGUI.EndChangeCheck())
            {
                assetViewMode = mode;
            }

            GUILayout.Space(4f);
        }

        public override void DrawGUI(params GUILayoutOption[] option)
        {
            if (EditorLayoutTools.DrawHeader(title, headerKey))
            {
                using (new ContentsScope())
                {
                    var scrollViewHeight = Mathf.Min(Contents.Length * 18f, 150f);

                    var options = new List<GUILayoutOption>();

                    options.Add(GUILayout.Height(scrollViewHeight));
                    options.AddRange(option);  

                    base.DrawGUI(options.ToArray());
                }
            }
        }

        private static bool CheckParentFolderRegisted(Object folder, FolderInfo[] folderInfos)
        {
            var assetPath = AssetDatabase.GetAssetPath(folder);

            var registedFolderInfo = folderInfos
                .Where(x => x.asset != null && !string.IsNullOrEmpty(x.assetPath))
                .FirstOrDefault(x => x.assetPath.Length <= assetPath.Length && assetPath.StartsWith(x.assetPath));

            if (registedFolderInfo != null)
            {
                EditorUtility.DisplayDialog("Register failed", "This folder is registed.", "Close");

                EditorGUIUtility.PingObject(registedFolderInfo.asset);

                return false;
            }

            return true;
        }

        // 親フォルダが登録された際に削除されるフォルダ情報取得.
        private static FolderInfo[] GetRemoveChildrenFolders(Object folder, FolderInfo[] folderInfos)
        {
            if (folder == null) { return new FolderInfo[0]; }

            var assetPath = AssetDatabase.GetAssetPath(folder);

            return folderInfos.Where(x => assetPath.Length < x.assetPath.Length && x.assetPath.StartsWith(assetPath)).ToArray();
        }

        private static bool CheckFolderAsset(Object folder)
        {
            if (folder == null) { return true; }

            var assetPath = AssetDatabase.GetAssetPath(folder);

            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                EditorUtility.DisplayDialog("Register failed", "This asset not a folder.", "Close");

                return false;
            }

            return true;
        }
    }
}
