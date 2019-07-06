
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;

namespace Modules.Devkit.AssetTuning
{
    public class SpriteAtlasAssetTuner : AssetTuner
    {
        //----- params -----

        private static readonly BuildTargetGroup[] DefaultTargetPlatforms =
        {
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
            spriteAtlas.SetIncludeInBuild(false);

            //------- PackingSettings -------

            var packingSettings = spriteAtlas.GetPackingSettings();

            packingSettings.padding = 2;

            spriteAtlas.SetPackingSettings(packingSettings);

            //------- TextureSettings -------

            var textureSettings = spriteAtlas.GetTextureSettings();

            textureSettings.readable = false;
            textureSettings.generateMipMaps = false;

            spriteAtlas.SetTextureSettings(textureSettings);

            //------ PlatformSettings ------
            
            foreach (var platformName in DefaultTargetPlatforms)
            {
                var platformSetting = spriteAtlas.GetPlatformSettings(platformName.ToString());

                platformSetting.overridden = true;
                platformSetting.format = TextureImporterFormat.ASTC_RGBA_4x4;
                platformSetting.maxTextureSize = 1024;

                spriteAtlas.SetPlatformSettings(platformSetting);
            }
        }
    }
}
