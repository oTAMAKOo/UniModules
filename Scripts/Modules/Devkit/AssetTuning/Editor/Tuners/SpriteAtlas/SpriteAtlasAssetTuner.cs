
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
            var spriteAtlasImporter = AssetImporter.GetAtPath(path) as SpriteAtlasImporter;

            return spriteAtlasImporter != null;
        }

        public override void OnAssetCreate(string path)
        {
            var spriteAtlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path);

            if (spriteAtlas == null) { return; }

            OnFirstImport(spriteAtlas);

            AssetDatabase.WriteImportSettingsIfDirty(path);
        }

        protected virtual void OnFirstImport(SpriteAtlas spriteAtlas)
        {
            var assetPath = AssetDatabase.GetAssetPath(spriteAtlas);

            var spriteAtlasImporter = AssetImporter.GetAtPath(assetPath) as SpriteAtlasImporter;

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

            foreach (var platform in DefaultTargetPlatforms)
            {
                var platformSetting = spriteAtlasImporter.GetPlatformSettings(platform.ToString());

                SetTexturePlatformSettings(platform, ref platformSetting);

                spriteAtlasImporter.SetPlatformSettings(platformSetting);
            }
        }

        protected virtual void SetPackingSettings(ref SpriteAtlasPackingSettings packingSettings)
        {
            packingSettings.padding = 2;
            packingSettings.enableTightPacking = false;
            packingSettings.enableRotation = false;

            #if UNITY_2022_1_OR_NEWER

            packingSettings.enableAlphaDilation = true;

            #endif
        }

        protected virtual void SetTextureSettings(ref SpriteAtlasTextureSettings textureSettings)
        {
            textureSettings.readable = false;
            textureSettings.generateMipMaps = false;
            textureSettings.filterMode = FilterMode.Bilinear;
        }

        protected virtual void SetTexturePlatformSettings(BuildTargetGroup platform, ref TextureImporterPlatformSettings platformSetting)
        {
            platformSetting.overridden = true;
            platformSetting.maxTextureSize = 2048;
            platformSetting.compressionQuality = 100;
            platformSetting.textureCompression = TextureImporterCompression.Compressed;

            switch (platform)
            {
                case BuildTargetGroup.Standalone:
                    platformSetting.format = TextureImporterFormat.DXT5;
                    break;

                case BuildTargetGroup.Android:
                case BuildTargetGroup.iOS:
                    platformSetting.format = TextureImporterFormat.ASTC_6x6;
                    break;
            }
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
