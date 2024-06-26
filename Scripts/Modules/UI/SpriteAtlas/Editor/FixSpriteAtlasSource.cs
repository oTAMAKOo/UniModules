
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
using System.Collections.Generic;
using Extensions.Devkit;
using Modules.Devkit.Console;

namespace Modules.Devkit.UI
{
    public static class FixSpriteAtlasSource
    {
        //----- params -----

        public static readonly BuildTargetGroup[] DefaultTargetPlatforms =
        {
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
            BuildTargetGroup.Standalone,
        };

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary> 全SpriteAtlasを編集 </summary>
        public static void Modify(BuildTargetGroup[] platforms)
        {
            using (new AssetEditingScope())
            {
                var allSpriteAtlas = UnityEditorUtility.FindAssetsByType<SpriteAtlas>("t:SpriteAtlas");

                foreach (var spriteAtlas in allSpriteAtlas)
                {
                    ModifySpriteAtlas(spriteAtlas, platforms);
                }
            }
        }

        /// <summary> 対象SpriteAtlasを編集 </summary>
        private static void ModifySpriteAtlas(SpriteAtlas spriteAtlas, BuildTargetGroup[] platforms)
        {
            var items = spriteAtlas.GetPackables();

            var targetAssets = new List<string>();

            foreach (var item in items)
            {
                var packableAssetPath = AssetDatabase.GetAssetPath(item);

                if (AssetDatabase.IsValidFolder(packableAssetPath))
                {
                    var assetGuids = AssetDatabase.FindAssets("*", new string[] { packableAssetPath });

                    foreach (var assetGuid in assetGuids)
                    {
                        var assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);

                        targetAssets.Add(assetPath);
                    }
                }
                else
                {
                    targetAssets.Add(packableAssetPath);
                }
            }

            var changed = false;

            foreach (var targetAsset in targetAssets)
            {
                changed |= ModifySpriteAtlasSource(targetAsset, platforms);
            }

            if (changed)
            {
                var assetPath = AssetDatabase.GetAssetPath(spriteAtlas);

                UnityConsole.Info($"SpriteAtlas source texture modify.\n{assetPath}");
            }
        }

        /// <summary> SpriteAtlasのソーステクスチャの編集 </summary>
        private static bool ModifySpriteAtlasSource(string assetPath, BuildTargetGroup[] platforms)
        {
            var textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (textureImporter == null) { return false; }

            var modify = false;

            //------ デフォルト設定 ------

            var defaultPlatformTextureSetting = textureImporter.GetDefaultPlatformTextureSettings();

            modify |= ModifyPlatformTextureSettings(textureImporter, defaultPlatformTextureSetting);

            //------ プラットフォーム設定 ------

            foreach (var platform in platforms)
            {
                var textureSettings = textureImporter.GetPlatformTextureSettings(platform.ToString());

                modify |= ModifyPlatformTextureSettings(textureImporter, textureSettings);
            }

            if (modify)
            {
                textureImporter.SaveAndReimport();
            }

            return modify;
        }

        private static bool ModifyPlatformTextureSettings(TextureImporter textureImporter, TextureImporterPlatformSettings textureSettings)
        {
            var changed = false;

            if (textureSettings.overridden)
            {
                textureSettings.overridden = false;
                changed = true;
            }

            if (textureSettings.format != TextureImporterFormat.Automatic)
            {
                textureSettings.format = TextureImporterFormat.Automatic;
                changed = true;
            }

            if (textureSettings.textureCompression != TextureImporterCompression.Uncompressed)
            {
                textureSettings.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (changed)
            {
                textureImporter.SetPlatformTextureSettings(textureSettings);
            }

            return changed;
        }
    }
}