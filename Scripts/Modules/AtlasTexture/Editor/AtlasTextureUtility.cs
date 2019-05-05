
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;
using UnityEditor.Experimental.U2D;
using Object = UnityEngine.Object;

namespace Modules.AtlasTexture
{
    public static class AtlasTextureUtility
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static void OpenSpriteEditorWindow(Sprite sprite)
        {
            Object editTarget = null;

            if (sprite == null) { return; }

            if (sprite.texture == null) { return; }

            var textureSprites = GetTextureSprites(sprite.texture).ToArray();

            if (textureSprites.Any())
            {
                editTarget = textureSprites.FirstOrDefault(x => x.GetSpriteID().ToString() == sprite.GetSpriteID().ToString());
            }
            else
            {
                editTarget = sprite.texture;
            }

            if (editTarget == null)
            {
                Debug.LogErrorFormat("Sprite : {0} not found.", sprite.name);
                return;
            }

            var prevActiveObject = Selection.activeObject;

            Selection.activeObject = editTarget;

            var spriteEditorWindowType = typeof(EditorWindow).Assembly.GetTypes().FirstOrDefault(t => t.Name == "SpriteEditorWindow");
            var spriteEditorWindow = EditorWindow.GetWindow(spriteEditorWindowType);

            if (spriteEditorWindow != null)
            {
                spriteEditorWindow.Show();
            }

            Selection.activeObject = prevActiveObject;
        }

        public static IEnumerable<Sprite> GetTextureSprites(Texture texture)
        {
            if (texture == null) { return new Sprite[0]; }

            var textureAssetPath = AssetDatabase.GetAssetPath(texture);

            return AssetDatabase.LoadAllAssetsAtPath(textureAssetPath).OfType<Sprite>().ToArray();
        }

        public static void ModifyTexture(Object asset)
        {
            if (asset == null) { return; }

            if (UnityEditorUtility.IsFolder(asset))
            {
                var assetPath = AssetDatabase.GetAssetPath(asset);

                var list = UnityEditorUtility.FindAssetsByType<Texture>("t:texture", new[] { assetPath });

                foreach (var item in list)
                {
                    ModifyTextureTypeToSprite(item);
                }
            }

            var texture = asset as Texture;

            if (texture != null)
            {
                ModifyTextureTypeToSprite(texture);
            }
        }

        private static void ModifyTextureTypeToSprite(Texture texture)
        {
            var textureAssetPath = AssetDatabase.GetAssetPath(texture);
            var textureImporter = AssetImporter.GetAtPath(textureAssetPath) as TextureImporter;

            if (textureImporter.textureType == TextureImporterType.Default)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.SaveAndReimport();
            }
        }
    }
}
