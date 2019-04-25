
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;

namespace Modules.Devkit.AssetTuning
{
    public abstract class SpriteAtlasAssetTuner : AssetTuner
    {
        public override int Priority { get { return 75; } }

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
        }
    }
}
