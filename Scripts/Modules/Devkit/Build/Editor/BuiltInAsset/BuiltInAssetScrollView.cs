
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using Modules.Cache;

using Object = UnityEngine.Object;

namespace Modules.Devkit.Build
{
	public sealed class BuiltInAssetScrollView : EditorGUIFastScrollView<BuiltInAssets.BuiltInAssetInfo>
    {
		//----- params -----

        public enum AssetErrorType
        {
            None = 0,

            /// <summary> 指定ディレクトリ以外のディレクトリに存在 </summary>
            [Label("DirectoryLocation")]
            InvalidDirectoryLocation,
            /// <summary> 含まれてはいけないディレクトリに存在 </summary>
            [Label("ForbiddenDirectory")]
            ForbiddenDirectory,
            /// <summary> 含まれていけないフォルダ名が含まれているか </summary>
            [Label("FolderName")]
            InvalidFolderName,
        }

		//----- field -----

		private AssetViewMode assetViewMode = AssetViewMode.Asset;

		private Cache<Object> assetCache = null;

		private Dictionary<string, AssetErrorType> invalidAssets = null;

		private string[] builtInAssetTargetPaths = null;
		private string[] ignoreBuiltInAssetTargetPaths = null;
		private string[] ignoreBuiltInFolderNames = null;
		private float warningAssetSize = 0f;

		//----- property -----

		public override Direction Type
		{
			get { return Direction.Vertical; }
		}

		//----- method -----

        public BuiltInAssetScrollView()
        {
			assetCache = new Cache<Object>();
            invalidAssets = new Dictionary<string, AssetErrorType>();
        }

		public void Setup()
		{
			var builtInAssetConfig = BuiltInAssetConfig.Instance;

			builtInAssetTargetPaths = builtInAssetConfig.BuiltInAssetTargets
                .Select(x => AssetDatabase.GetAssetPath(x))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

			ignoreBuiltInAssetTargetPaths = builtInAssetConfig.IgnoreBuiltInAssetTargets
                .Select(x => AssetDatabase.GetAssetPath(x))
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();
			
            ignoreBuiltInFolderNames = builtInAssetConfig.IgnoreBuiltInFolderNames
                .Where(x => !string.IsNullOrEmpty(x))
                .ToArray();

			warningAssetSize = builtInAssetConfig.WarningAssetSize;
		}

		public void SetAssetViewMode(AssetViewMode mode)
		{
			assetViewMode = mode;
		}

        protected override void DrawContent(int index, BuiltInAssets.BuiltInAssetInfo content)
        {
			using (new EditorGUILayout.HorizontalScope())
	        {
	            var asset = GetAsset(content.assetPath);

	            if (asset != null)
	            {
	                EditorGUILayout.LabelField(content.GetSizeText(), GUILayout.Width(70f));

	                EditorGUILayout.LabelField(string.Format("{0:F1}%", content.ratio), GUILayout.Width(50f));

	                switch (assetViewMode)
	                {
	                    case AssetViewMode.Asset:
	                        EditorGUILayout.ObjectField(asset, typeof(Object), false, GUILayout.MinWidth(250f));
	                        break;

	                    case AssetViewMode.Path:
	                        EditorGUILayout.DelayedTextField(content.assetPath, GUILayout.MinWidth(250f));
	                        break;
					}

					var assetErrorType = AssetErrorType.None;

					if (!invalidAssets.TryGetValue(content.assetPath, out var invalidAsset))
					{
                        // 指定ディレクトリ以外のディレクトリに存在.
                        if(!builtInAssetTargetPaths.Any(x => content.assetPath.StartsWith(x)))
                        {
                            assetErrorType = AssetErrorType.InvalidDirectoryLocation;
                        }
                        // 含まれてはいけないディレクトリに存在.
                        else if(ignoreBuiltInAssetTargetPaths.Any(x => content.assetPath.StartsWith(x)))
                        {
                            assetErrorType = AssetErrorType.ForbiddenDirectory;
                        }
                        // 含まれていけないフォルダ名が含まれているか.
                        else if(ignoreBuiltInFolderNames.Any(x => content.assetPath.Split('/').Contains(x)))
                        {
                            assetErrorType = AssetErrorType.InvalidFolderName;
                        }

						invalidAssets[content.assetPath] = assetErrorType;
					}
					else
					{
                        assetErrorType = invalidAsset;	
					}

					var titleStyle = new EditorLayoutTools.TitleGUIStyle();

	                // 指定されたAsset置き場にない or 同梱しないAsset置き場のAssetが混入.
	                if (assetErrorType != AssetErrorType.None)
	                {
	                    titleStyle.backgroundColor = Color.red;
	                    titleStyle.labelColor = Color.gray;
	                    titleStyle.width = 85f;

	                    EditorLayoutTools.Title(assetErrorType.ToLabelName(), titleStyle);
	                }

	                // ファイルサイズが指定された値を超えている.
	                if (warningAssetSize <= content.size)
	                {
	                    titleStyle.backgroundColor = Color.yellow;
	                    titleStyle.labelColor = Color.gray;
	                    titleStyle.width = 80f;

	                    EditorLayoutTools.Title("LargeAsset", titleStyle);
	                }

	                GUILayout.Space(5f);
	            }
	        }
        }

		private Object GetAsset(string assetPath)
		{
			if (assetCache == null)
			{
				assetCache = new Cache<Object>();
			}

			Object asset = null;

			if (assetCache.HasCache(assetPath))
			{
				asset = assetCache.Get(assetPath);
			}
			else
			{
				asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

				assetCache.Add(assetPath, asset);
			}

			return asset;
		}
    }
}