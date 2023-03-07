
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Modules.Devkit.ScriptableObjects;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.AssetTuning.TextureAsset
{
	public enum PlatformType
	{
		Default,
		Standalone,
		iOS,
		Android,
	}

	public sealed class TextureConfig : ReloadableScriptableObject<TextureConfig>
	{
        //----- params -----

		public static class Prefs
		{
			public static bool forceModifyOnImport
			{
				get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-forceModifyOnImport", false); }
				set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-forceModifyOnImport", value); }
			}
		}

		//----- field -----

		[SerializeField]
		private TextureData defaultData = null;
		[SerializeField]
		private TextureData[] customData = null;

		// フォルダパスをキーにしたキャッシュ.
		private Dictionary<string, TextureData> cache = null;

        //----- property -----

		public TextureData DefaultData
		{
			get { return defaultData ?? (defaultData = new TextureData()); }
		}

        //----- method -----

		protected override void OnLoadInstance()
		{
			BuildCache();
		}

		public TextureData CreateNewData()
		{
			var newData = DefaultData.DeepCopy();

			//------ Optionデータは引き継がない ------

			newData.ignoreFolders = new string[0];
			newData.ignoreFolderNames = new string[0];

			//----------------------------------------

			return newData;
		}

		public TextureData GetData(string assetPath)
		{
			BuildCache();

			assetPath = PathUtility.ConvertPathSeparator(assetPath);

			var data = cache
				.Where(x => assetPath.StartsWith(x.Key))
				.Where(x => !x.Value.IsIgnoreTarget(assetPath))
				.FindMax(x => x.Key.Length);

			return data.IsDefault() ? null : data.Value;
		}

		private void BuildCache()
		{
			if (cache != null) { return; }

			cache = new Dictionary<string, TextureData>();

			foreach (var item in customData)
			{
				var folderPath = AssetDatabase.GUIDToAssetPath(item.folderGuid);

				folderPath = PathUtility.ConvertPathSeparator(folderPath);

				cache[folderPath] = item;
			}
		}
	}
}