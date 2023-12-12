
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
using System.Linq;

namespace Modules.Devkit.AssetTuning
{
    public class SpriteAtlasAssetTuner : AssetTuner
    {
        //----- params -----

        private static readonly BuildTargetGroup[] DefaultTargetPlatforms =
        {
            BuildTargetGroup.Standalone,
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
        };

        //----- field -----

        //----- property -----

        public override int Priority { get { return 75; } }

        //----- method -----

        public override bool Validate(string path)
        {
            var spriteAtlas = AssetDatabase.LoadMainAssetAtPath(path) as SpriteAtlas;

            return spriteAtlas != null;
        }

        public override void OnAssetCreate(string path)
        {
            var spriteAtlas = AssetDatabase.LoadMainAssetAtPath(path) as SpriteAtlas;

            if (spriteAtlas == null) { return; }

            OnFirstImport(spriteAtlas);

            AssetDatabase.WriteImportSettingsIfDirty(path);
        }

        protected virtual void OnFirstImport(SpriteAtlas spriteAtlas)
        {
			SetIncludeInBuild(spriteAtlas);

			//------- PackingSettings -------

            var packingSettings = spriteAtlas.GetPackingSettings();

            SetPackingSettings(ref packingSettings);

            spriteAtlas.SetPackingSettings(packingSettings);

            //------- TextureSettings -------

            var textureSettings = spriteAtlas.GetTextureSettings();

            SetTextureSettings(ref textureSettings);

            spriteAtlas.SetTextureSettings(textureSettings);

            //------ PlatformSettings ------

            foreach (var platformName in DefaultTargetPlatforms)
            {
                var platformSetting = spriteAtlas.GetPlatformSettings(platformName.ToString());

                SetTexturePlatformSettings(ref platformSetting);

                spriteAtlas.SetPlatformSettings(platformSetting);
            }
        }

        protected virtual void SetPackingSettings(ref SpriteAtlasPackingSettings packingSettings)
        {
            packingSettings.padding = 2;
            packingSettings.enableTightPacking = false;
            packingSettings.enableRotation = false;
        }

        protected virtual void SetTextureSettings(ref SpriteAtlasTextureSettings textureSettings)
        {
            textureSettings.readable = false;
            textureSettings.generateMipMaps = false;
            textureSettings.filterMode = FilterMode.Bilinear;
        }

        protected virtual void SetTexturePlatformSettings(ref TextureImporterPlatformSettings platformSetting)
        {
            platformSetting.overridden = true;
            platformSetting.format = TextureImporterFormat.ASTC_6x6;
            platformSetting.maxTextureSize = 2048;
            platformSetting.compressionQuality = 100;
            platformSetting.textureCompression = TextureImporterCompression.Compressed;
        }

		private void SetIncludeInBuild(SpriteAtlas spriteAtlas)
		{
			var config = SpriteAtlasConfig.Instance;

			if (config == null){ return; }

			var assetPath = AssetDatabase.GetAssetPath(spriteAtlas);

			var targetFolderPaths =  config.DisableIncludeInBuildFolders
				.Where(x => x != null)
				.Select(x => AssetDatabase.GetAssetPath(x))
				.OrderBy(x => x.Length)
				.ToArray();

			var includeInBuild = true;

			foreach (var folderPath in targetFolderPaths)
			{
				if (assetPath.StartsWith(folderPath))
				{
					includeInBuild = false;
					break;
				}
			}

			spriteAtlas.SetIncludeInBuild(includeInBuild);
		}
    }
}
