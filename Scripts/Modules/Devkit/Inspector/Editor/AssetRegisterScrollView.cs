
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using UniRx;
using Extensions.Devkit;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Inspector
{
    public class AssetRegisterScrollView : RegisterScrollView<AssetRegisterScrollView.AssetInfo>
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

        /// <summary>
        /// スクロールビューの高さ.
        /// </summary>
        public float ScrollAreaHeight { get; private set; } 

        //----- method -----

        public AssetRegisterScrollView(string title, string headerKey, float scrollAreaHeight = 270)
        {
            this.title = title;
            this.headerKey = headerKey;

            ScrollAreaHeight = scrollAreaHeight;

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

        // 外部公開しない.
        private new void SetContents(AssetInfo[] contents)
		{
			base.SetContents(contents);
		}

        public void SetContents(string[] guids)
        {
            if (guids == null)
            {
                guids = new string[0];
            }

            Func<string, AssetInfo> createAssetInfoFromGuid = guid =>
            {
                var assetPath = guid != null ? AssetDatabase.GUIDToAssetPath(guid) : string.Empty;

                var asset = string.IsNullOrEmpty(assetPath) ? null : AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));

                var info = new AssetInfo()
                {
                    guid = guid,
                    assetPath = assetPath,
                    asset = asset,
                };

                return info;
            };

            var assetInfos = guids.Select(x => createAssetInfoFromGuid.Invoke(x)).ToArray();

			SetContents(assetInfos);
        }

        protected override AssetInfo DrawContent(Rect rect, int index, AssetInfo info)
        {
            switch (assetViewMode)
            {
                case AssetViewMode.Asset:
                    {
                        EditorGUI.BeginChangeCheck();

                        var asset = EditorGUI.ObjectField(rect, info.asset, typeof(Object), false);

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
                        }
                    }
                    break;

                case AssetViewMode.Path:
                    EditorGUI.LabelField(rect, info.assetPath, EditorStyles.textArea);
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

        public override void DrawGUI()
        {
            if (EditorLayoutTools.Header(title, headerKey))
            {
                using (new ContentsScope())
                {
					base.DrawGUI();
                }
            }
        }

        // 登録された際に削除されるアセット情報取得.
        protected override void ValidateContent(AssetInfo newAssetInfo)
        {
            // 親が登録された場合子階層を除外.
            if (RemoveChildrenAssets)
            {
                var removeInfos = new List<AssetInfo>();

                // 同じオブジェクトが複数登録済み.

                var infos = Contents.Where(x => x.guid == newAssetInfo.guid).ToArray();

                if (1 < infos.Length)
                {
                    for (var i = 1; i < infos.Length; i++)
                    {
                        removeInfos.Add(infos[i]);
                    }
                }

                // 親階層のフォルダが既に登録済み.

                var folderInfos = Contents.Where(x => AssetDatabase.IsValidFolder(x.assetPath));

                foreach (var folderInfo in folderInfos)
                {
                    if (newAssetInfo.assetPath.Length <= folderInfo.assetPath.Length) { continue; }

                    if (newAssetInfo.assetPath.StartsWith(folderInfo.assetPath))
                    {
                        removeInfos.Add(newAssetInfo);
                    }
                }

                // 子が登録済み.

                if (AssetDatabase.IsValidFolder(newAssetInfo.assetPath))
                {
                    foreach (var assetInfo in Contents)
                    {
                        if (assetInfo.assetPath.Length <= newAssetInfo.assetPath.Length) { continue; }

                        if (assetInfo.assetPath.StartsWith(newAssetInfo.assetPath))
                        {
                            removeInfos.Add(assetInfo);
                        }
                    }
                }

                // 除外対象を除外.

                if (removeInfos.Any())
                {
                    EditorUtility.DisplayDialog("Removed Assets", "Removed registration of child assets.", "Close");

                    foreach (var info in removeInfos)
                    {
                        var content = contents.FirstOrDefault(x => x.guid == info.guid);

                        if (content != null)
                        {
                            contents.Remove(content);
                        }
                    }
                }
            }
        }
    }
}
