
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Inspector
{
    public sealed class AssetRegisterScrollView : RegisterScrollView<AssetRegisterScrollView.AssetInfo>
    {
        //----- params -----

        private enum AssetViewMode
        {
            Asset,
            Path
        }

        public sealed class AssetInfo
        {
            public string assetPath = null;
            public string guid = null;
            public Object asset = null;
        }

        //----- field -----

        private string title = null;
        private string headerKey = null;

        private AssetViewMode assetViewMode = AssetViewMode.Asset;

        private Subject<string[]> onUpdateContents = null;

        //----- property -----

        /// <summary>
        /// 既に子階層のフォルダが登録済みの時そのフォルダを除外するか.
        /// </summary>
        public bool RemoveChildrenAssets { get; set; }

        //----- method -----

        public AssetRegisterScrollView(string title, string headerKey)
        {
            this.title = title;
            this.headerKey = headerKey;

            OnUpdateContentsAsObservable()
                .Subscribe(x =>
                {
                    if (onUpdateContents != null)
                    {
                        var folders = x.Select(y => y.guid).ToArray();

                        onUpdateContents.OnNext(folders);
                    }
                })
                .AddTo(Disposable);

            RemoveChildrenAssets = false;
        }

        public void SetContents(string[] guids)
        {
            if (guids == null)
            {
                guids = new string[0];
            }

            Contents = guids
                .Select(x =>
                {
                    var assetPath = x != null ? AssetDatabase.GUIDToAssetPath(x) : string.Empty;
                    var asset = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));

                    var info = new AssetInfo()
                    {
                        guid = x,
                        assetPath = assetPath,
                        asset = asset,
                    };

                    return info;
                })
                .ToArray();
        }

        protected override AssetInfo DrawContent(int index, AssetInfo info)
        {
            switch (assetViewMode)
            {
                case AssetViewMode.Asset:
                    {
                        EditorGUI.BeginChangeCheck();

                        var asset = EditorGUILayout.ObjectField(info.asset, typeof(Object), false);

                        if (EditorGUI.EndChangeCheck())
                        {
                            var assetPath = AssetDatabase.GetAssetPath(asset);
                            var guid = AssetDatabase.AssetPathToGUID(assetPath);

                            info = new AssetInfo()
                            {
                                guid = guid,
                                assetPath = assetPath,
                                asset = asset,
                            };

                            // 一旦ローカル配列に変換してから上書き.
                            var contents = Contents.ToArray();

                            contents[index] = info;

                            // 親が登録された場合子階層を除外.
                            if (RemoveChildrenAssets)
                            {
                                if (CheckParentFolderRegisted(asset, Contents))
                                {
                                    var removeChildrenInfos = GetRemoveChildrenAssets(asset, Contents);

                                    if (removeChildrenInfos.Any())
                                    {
                                        EditorUtility.DisplayDialog("Removed Assets", "Removed registration of child assets.", "Close");

                                        contents = contents.Where(x => !removeChildrenInfos.Contains(x)).ToArray();
                                    }
                                }
                            }

                            Contents = contents;
                        }
                    }
                    break;

                case AssetViewMode.Path:
                    GUILayout.Label(info.assetPath, EditorStyles.textArea);
                    break;
            }

            return info;
        }

        protected override AssetInfo CreateNewContent()
        {
            var assetInfo = new AssetInfo()
            {
                guid = null,
                asset = null,
                assetPath = string.Empty,
            };

            return assetInfo;
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
            if (EditorLayoutTools.Header(title, headerKey))
            {
                using (new ContentsScope())
                {
                    var scrollViewHeight = Mathf.Min(Contents.Length * 20f, 150f);

                    var options = new List<GUILayoutOption>();

                    options.Add(GUILayout.Height(scrollViewHeight));
                    options.AddRange(option);

                    base.DrawGUI(options.ToArray());
                }
            }
        }

        protected static bool CheckParentFolderRegisted(Object asset, AssetInfo[] assetInfos)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);

            var registedAssetInfo = assetInfos
                .Where(x => x.asset != null && !string.IsNullOrEmpty(x.assetPath))
                .FirstOrDefault(x => x.assetPath.Length <= assetPath.Length && assetPath.StartsWith(x.assetPath));

            if (registedAssetInfo != null)
            {
                EditorUtility.DisplayDialog("Register failed", "This folder is registed.", "Close");

                EditorGUIUtility.PingObject(registedAssetInfo.asset);

                return false;
            }

            return true;
        }

        // 親フォルダが登録された際に削除されるアセット情報取得.
        private static AssetInfo[] GetRemoveChildrenAssets(Object asset, AssetInfo[] assetInfos)
        {
            if (asset == null) { return new AssetInfo[0]; }

            var assetPath = AssetDatabase.GetAssetPath(asset);

            if (!AssetDatabase.IsValidFolder(assetPath)) { return new AssetInfo[0]; }

            return assetInfos.Where(x => assetPath.Length < x.assetPath.Length && x.assetPath.StartsWith(assetPath)).ToArray();
        }
    }
}
