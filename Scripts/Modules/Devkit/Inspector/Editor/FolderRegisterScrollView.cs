
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
    public sealed class FolderRegisterScrollView : LifetimeDisposable
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

        private sealed class FolderScrollView : EditorGUIFastScrollView<FolderInfo>
        {
            private GUIContent toolbarPlusIcon = null;

            private Subject<FolderInfo[]> onUpdateContents = null;

            public AssetViewMode AssetViewMode { get; set; }
            public bool RemoveChildrenFolder { get; set; }

            public override Direction Type { get { return Direction.Vertical; } }

            public FolderScrollView() : base()
            {
                toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Minus");
            }

            protected override void DrawContent(int index, FolderInfo content)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();

                    switch (AssetViewMode)
                    {
                        case AssetViewMode.Asset:
                            {
                                EditorGUI.BeginChangeCheck();

                                var folder = EditorGUILayout.ObjectField(content.asset, typeof(Object), false);

                                if (EditorGUI.EndChangeCheck())
                                {
                                    if (CheckFolderAsset(folder))
                                    {
                                        var newContent = new FolderInfo()
                                        {
                                            asset = folder,
                                            assetPath = AssetDatabase.GetAssetPath(folder),
                                        };

                                        // 一旦ローカル配列に変換してから上書き.
                                        var contents = Contents.ToArray();

                                        contents[index] = newContent;

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

                                        UpdateContens();
                                    }
                                }
                            }
                            break;

                        case AssetViewMode.Path:
                            GUILayout.Label(content.assetPath, EditorLayoutTools.TextAreaStyle);
                            break;
                    }

                    if (GUILayout.Button(toolbarPlusIcon, EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
                    {
                        var list = Contents.ToList();

                        list.RemoveAt(index);

                        Contents = list.ToArray();

                        UpdateContens();
                    }
                }
            }

            private void UpdateContens()
            {
                if (onUpdateContents != null)
                {
                    onUpdateContents.OnNext(Contents);
                }

                RequestRepaint();
            }

            public IObservable<FolderInfo[]> OnUpdateContentsAsObservable()
            {
                return onUpdateContents ?? (onUpdateContents = new Subject<FolderInfo[]>());
            }
        }

        //----- field -----

        private string title = null;
        private string headerKey = null;
        private AssetViewMode assetViewMode = AssetViewMode.Asset;
        private List<FolderInfo> folderInfos = null;
        private FolderScrollView folderScrollView = null;
        private GUIContent toolbarPlusIcon = null;

        private Subject<Unit> onRepaintRequest = null;
        private Subject<Object[]> onUpdateContents = null;

        //----- property -----

        public bool RemoveChildrenFolder
        {
            get { return folderScrollView.RemoveChildrenFolder; }
            set { folderScrollView.RemoveChildrenFolder = value; }
        }

        //----- method -----

        public FolderRegisterScrollView(string title, string headerKey)
        {
            this.title = title;

            folderScrollView = new FolderScrollView();
            folderScrollView.AssetViewMode = assetViewMode;

            folderScrollView.OnUpdateContentsAsObservable()
                .Subscribe(x =>
                    {
                        folderInfos = x.ToList();

                        if (onUpdateContents != null)
                        {
                            var folders = x.Select(y => y.asset).ToArray();

                            onUpdateContents.OnNext(folders);
                        }
                    })
                .AddTo(Disposable);

            folderScrollView.OnRepaintRequestAsObservable()
                .Subscribe(_ =>
                   {
                       if (onRepaintRequest != null)
                       {
                           onRepaintRequest.OnNext(Unit.Default);
                       }
                   })
                .AddTo(Disposable);

            RemoveChildrenFolder = false;

            toolbarPlusIcon = EditorGUIUtility.IconContent("Toolbar Plus");
        }

        public void SetContents(Object[] folders)
        {
            folderInfos = folders
                .Select(x =>
                    {
                        var info = new FolderInfo()
                        {
                            asset = x,
                            assetPath = x != null ? AssetDatabase.GetAssetPath(x) : string.Empty,
                        };

                        return info;
                    })
                .ToList();

            folderScrollView.Contents = folderInfos.ToArray();            
        }

        public void DrawGUI()
        {
            if (EditorLayoutTools.DrawHeader(title, headerKey))
            {
                using (new ContentsScope())
                {
                    GUILayout.Space(2f);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();

                        assetViewMode = (AssetViewMode)EditorGUILayout.EnumPopup(assetViewMode, GUILayout.Width(60f));

                        if (EditorGUI.EndChangeCheck())
                        {
                            folderScrollView.AssetViewMode = assetViewMode;
                        }

                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(toolbarPlusIcon, EditorStyles.miniButton, GUILayout.Width(24f), GUILayout.Height(15f)))
                        {
                            var compressFolder = new FolderInfo()
                            {
                                asset = null,
                                assetPath = string.Empty,
                            };

                            folderInfos.Add(compressFolder);
                            
                            folderScrollView.Contents = folderInfos.ToArray();

                            if (onUpdateContents != null)
                            {
                                var folders = folderInfos.Select(x => x.asset).ToArray();

                                onUpdateContents.OnNext(folders);
                            }
                        }
                    }

                    GUILayout.Space(4f);

                    var scrollViewHeight = Mathf.Min(folderInfos.Count * 18f, 150f);

                    folderScrollView.Draw(true, GUILayout.Height(scrollViewHeight));
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

        public IObservable<Unit> OnRepaintRequestAsObservable()
        {
            return onRepaintRequest ?? (onRepaintRequest = new Subject<Unit>());
        }

        public IObservable<Object[]> OnUpdateContentsAsObservable()
        {
            return onUpdateContents ?? (onUpdateContents = new Subject<Object[]>());
        }
    }
}
