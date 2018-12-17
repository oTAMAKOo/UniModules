
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;

namespace Modules.Devkit.AssetTuning
{
    public abstract class SpriteAtlasAssetTuner : IAssetTuner
    {
        public bool Validate(string path)
        {
            var spriteAtlas = AssetDatabase.LoadMainAssetAtPath(path) as SpriteAtlas;

            return spriteAtlas != null;
        }

        public void OnAssetCreate(string path)
        {
            var spriteAtlas = AssetDatabase.LoadMainAssetAtPath(path) as SpriteAtlas;

            if (spriteAtlas == null) { return; }

            OnFirstImport(spriteAtlas);

            AssetDatabase.WriteImportSettingsIfDirty(path);
        }

        public virtual void OnAssetImport(string path) { }

        public virtual void OnAssetDelete(string path) { }

        public virtual void OnAssetMove(string path, string from) { }

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
