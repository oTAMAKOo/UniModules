
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.ScriptableObjects;

namespace Modules.Devkit.AssetTuning
{
	[Serializable]
	public sealed class CompressInfo
	{
		[SerializeField]
		public bool isOverride = false;

		[SerializeField]
		public int maxSize = 1024;

		[SerializeField]
		public TextureResizeAlgorithm resizeAlgorithm = TextureResizeAlgorithm.Mitchell;

		[SerializeField]
		public TextureImporterFormat format = TextureImporterFormat.Automatic;

		[SerializeField]
		public int compressionQuality = 50;
	}

	[Serializable]
	public sealed class CompressSetting
	{
		[SerializeField]
		public string folderGuid = null;

		[SerializeField]
		public CompressInfo iosSetting = new CompressInfo();

		[SerializeField]
		public CompressInfo androidSetting = new CompressInfo();
			
		[SerializeField]
		public CompressInfo standaloneSetting = new CompressInfo();

		public CompressInfo GetCompressInfo(BuildTargetGroup buildTargetGroup)
		{
			switch (buildTargetGroup)
			{
				case BuildTargetGroup.iOS:        return iosSetting;
				case BuildTargetGroup.Android:    return androidSetting;
				case BuildTargetGroup.Standalone: return standaloneSetting;
			}

			return null;
		}
	}

	public sealed class TextureCompressConfig : ReloadableScriptableObject<TextureCompressConfig>
	{
        //----- params -----

		//----- field -----

		[SerializeField]
		private CompressSetting defaultSetting = null;
		[SerializeField]
		private CompressSetting[] compressSettings = null;

		// フォルダパスをキーにしたキャッシュ.
		private Dictionary<string, CompressSetting> cache = null;

        //----- property -----

		public CompressSetting DefaultSetting
		{
			get { return defaultSetting ?? (defaultSetting = new CompressSetting()); }
		}

        //----- method -----

		protected override void OnLoadInstance()
		{
			BuildCache();
		}

		public CompressSetting CreateNewCompressSetting()
		{
			return DefaultSetting.DeepCopy();
		}

		public CompressSetting GetCompressSetting(string assetPath)
		{
			BuildCache();

			assetPath = PathUtility.ConvertPathSeparator(assetPath);

			var setting = cache.Where(x => assetPath.StartsWith(x.Key)).FindMax(x => x.Key.Length);

			return setting.IsDefault() ? null : setting.Value;
		}

		private void BuildCache()
		{
			if (cache != null) { return; }

			cache = new Dictionary<string, CompressSetting>();

			foreach (var item in compressSettings)
			{
				var folderPath = AssetDatabase.GUIDToAssetPath(item.folderGuid);

				folderPath = PathUtility.ConvertPathSeparator(folderPath);

				cache[folderPath] = item;
			}
		}
    }
}