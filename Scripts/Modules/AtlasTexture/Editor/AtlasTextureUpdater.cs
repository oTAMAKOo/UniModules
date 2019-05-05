
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditor.U2D;
using UnityEditor.Experimental.U2D;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Extensions;
using Extensions.Devkit;

namespace Modules.AtlasTexture
{
    public static class AtlasTextureUpdater
    {
        public static void UpdateAllAtlasTextureSpriteData()
        {
            var assets = UnityEditorUtility.FindAssetsByType<AtlasTexture>("t:AtlasTexture");

            AssetDatabase.StartAssetEditing();

            foreach (var asset in assets)
            {
                SetAtlasSpriteData(asset);
            }

            AssetDatabase.StopAssetEditing();
        }

        public static void SetAtlasSpriteData(AtlasTexture atlasTexture)
        {
            if (atlasTexture == null) { return; }

            var spriteAtlas = Reflection.GetPrivateField<AtlasTexture, SpriteAtlas>(atlasTexture, "spriteAtlas");

            if (spriteAtlas == null) { return; }

            SpriteAtlasUtility.PackAtlases(new SpriteAtlas[] { spriteAtlas }, EditorUserBuildSettings.activeBuildTarget, false);

            var sprites = spriteAtlas.GetAllSprites().ToArray();

            var list = new List<AtlasTexture.AtlasSpriteData>();

            foreach (var sprite in sprites)
            {
                var spriteName = sprite.name;
                var spriteGuid = sprite.GetSpriteID().ToString();

                var item = new AtlasTexture.AtlasSpriteData(spriteName, spriteGuid);

                list.Add(item);
            }

            Reflection.SetPrivateField(atlasTexture, "spriteData", list.ToArray());

            UnityEditorUtility.SaveAsset(atlasTexture);
        }

        public static void UpdateAtlasTextureImage(AtlasTexture atlasTexture)
        {
            var gameObjects = UnityEditorUtility.FindAllObjectsInHierarchy();

            var atlasTexturePath = AssetDatabase.GetAssetPath(atlasTexture);

            foreach (var gameObject in gameObjects)
            {
                var atlasTextureImage = UnityUtility.GetComponent<AtlasTextureImage>(gameObject);

                if (atlasTextureImage != null && atlasTextureImage.AtlasTexture != null)
                {
                    var path = AssetDatabase.GetAssetPath(atlasTextureImage.AtlasTexture);

                    if (path == atlasTexturePath)
                    {
                        atlasTextureImage.Apply();
                    }
                }
            }
        }
    }
}
