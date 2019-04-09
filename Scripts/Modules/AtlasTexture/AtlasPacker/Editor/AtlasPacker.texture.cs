
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;

namespace Modules.Atlas
{
    public partial class AtlasPacker
    {
        //----- params -----

        internal class TextureData
        {
            public Texture2D texture = null;
            public List<SpriteMetaData> spritesheet = null;
            public float spritePixelsPerUnit = 1f;
        }

        //----- field -----
        
        private int padding = 0;
        private float pixelsPerUnit = 100f;
        private FilterMode filterMode = FilterMode.Bilinear;

        //----- property -----

        //----- method -----

        private void ExtractSprites(AtlasTexture atlas, List<SpriteEntry> finalSprites)
        {
            var tex = atlas.Texture as Texture2D;

            if (tex != null)
            {
                Color32[] pixels = null;

                var width = tex.width;
                var height = tex.height;
                var sprites = atlas.Sprites;
                var count = sprites.Count;
                var index = 0;

                foreach (var es in sprites)
                {
                    var progress = (float)index++ / count;
                    
                    EditorUtility.DisplayProgressBar("progress", "Updating the atlas...", progress);

                    var found = false;

                    foreach (var fs in finalSprites)
                    {
                        if (!string.IsNullOrEmpty(es.guid) && es.guid == fs.guid)
                        {
                            found = true;
                        }
                        else if (es.name == fs.name)
                        {
                            found = true;
                        }

                        if (found)
                        {
                            fs.CopyBorderFrom(es);
                            break;
                        }
                    }

                    if (!found)
                    {
                        if (pixels == null)
                        {
                            pixels = tex.GetPixels32();
                        }

                        var sprite = ExtractSprite(es, pixels, width, height);

                        if (sprite != null)
                        {
                            finalSprites.Add(sprite);
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private bool UpdateTexture(AtlasTexture atlas, List<SpriteEntry> sprites)
        {
            var texture = atlas.Texture as Texture2D;

            var oldPath = (texture != null) ? AssetDatabase.GetAssetPath(texture.GetInstanceID()) : string.Empty;
            var newPath = GetSaveableTexturePath(atlas);

            if (System.IO.File.Exists(newPath))
            {
                System.IO.FileAttributes newPathAttrs = System.IO.File.GetAttributes(newPath);
                newPathAttrs &= ~System.IO.FileAttributes.ReadOnly;
                System.IO.File.SetAttributes(newPath, newPathAttrs);
            }

            var newTexture = (texture == null || oldPath != newPath);

            if (newTexture)
            {
                texture = TextureUtility.CreateEmptyTexture(1, 1, TextureFormat.ARGB32);
            }
            else
            {
                RegisterEditableTexture(texture, true);
            }

            if (PackTextures(texture, sprites, padding))
            {
                var bytes = texture.EncodeToPNG();

                System.IO.File.WriteAllBytes(newPath, bytes);
                bytes = null;

                AssetDatabase.ImportAsset(newPath);

                texture = AssetDatabase.LoadMainAssetAtPath(newPath) as Texture2D;

                if (newTexture)
                {
                    Reflection.SetPrivateField<AtlasTexture, Texture>(atlas, "texture", texture);

                    ReleaseSprites(sprites);
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Operation Canceled", "The selected sprites can't fit into the atlas.", "OK");

                return false;
            }

            return true;
        }

        private static bool PackTextures(Texture2D texture, List<SpriteEntry> sprites, int padding)
        {
            #if UNITY_3_5 || UNITY_4_0
		    
            var maxSize = 4096;
            
            #else

            var maxSize = SystemInfo.maxTextureSize;

            #endif

            maxSize = Mathf.Min(maxSize, 2048);

            sprites.Sort(Compare);

            var textures = sprites.Select(x => x.texture).ToArray();

            var rects = texture.PackTextures(textures, padding, maxSize);

            for (var i = 0; i < sprites.Count; ++i)
            {
                var rect = TextureUtility.ConvertToPixels(rects[i], texture.width, texture.height, true);

                if (textures[i] == null)
                {
                    return false;
                }

                if (Mathf.RoundToInt(rect.width) != textures[i].width)
                {
                    return false;
                }

                var spriteEntry = sprites[i];

                spriteEntry.x = Mathf.RoundToInt(rect.x);
                spriteEntry.y = Mathf.RoundToInt(rect.y);
                spriteEntry.width = Mathf.RoundToInt(rect.width);
                spriteEntry.height = Mathf.RoundToInt(rect.height);
            }

            return true;
        }

        private void SaveTextureMetaData(Texture2D texture, List<SpriteEntry> sprites)
        {
            var spritesheet = new List<SpriteMetaData>();
            var path = AssetDatabase.GetAssetPath(texture.GetInstanceID());

            // TextureImporter.
            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            if (textureImporter != null)
            {
                textureImporter.textureType = TextureImporterType.Sprite;
                textureImporter.spriteImportMode = SpriteImportMode.Multiple;
                textureImporter.filterMode = filterMode;
                textureImporter.spritePixelsPerUnit = pixelsPerUnit;

                for (var i = 0; i < sprites.Count; ++i)
                {
                    var sprite = sprites[i];

                    var metaData = new SpriteMetaData();

                    var rect = new Rect(new Vector2(sprite.x, sprite.y), new Vector2(sprite.width, sprite.height));

                    metaData.name = sprite.name;
                    metaData.rect = AtlasTexture.ConvertToSpriteSheetPixels(rect, texture.width, texture.height);

                    spritesheet.Add(metaData);
                }

                textureImporter.spritesheet = spritesheet.ToArray();

                // TextureImporterSettings.
                var textureImporterSettings = new TextureImporterSettings();
                textureImporter.ReadTextureSettings(textureImporterSettings);

                textureImporterSettings.spriteMeshType = SpriteMeshType.FullRect;
                textureImporterSettings.readable = false;
                textureImporterSettings.spriteGenerateFallbackPhysicsShape = false;

                textureImporter.SetTextureSettings(textureImporterSettings);
                
                AssetDatabase.ImportAsset(path);
            }
        }

        private SpriteData AddSprite(List<SpriteData> sprites, SpriteEntry se)
        {
            foreach (var sp in sprites)
            {
                if (sp.name == se.name)
                {
                    sp.CopyFrom(se);
                    return sp;
                }
            }

            var sprite = new SpriteData();
            sprite.CopyFrom(se);
            sprites.Add(sprite);

            return sprite;
        }

        private void ReleaseSprites(List<SpriteEntry> sprites)
        {
            foreach (SpriteEntry se in sprites)
            {
                se.Release();
            }

            Resources.UnloadUnusedAssets();
        }

        private void ReplaceSprites(AtlasTexture atlas, List<SpriteEntry> sprites)
        {
            var spriteList = atlas.Sprites;
            var kept = new List<SpriteData>();

            for (var i = 0; i < sprites.Count; ++i)
            {
                var se = sprites[i];
                var sprite = AddSprite(spriteList, se);
                kept.Add(sprite);
            }

            for (int i = spriteList.Count; i > 0;)
            {
                var sp = spriteList[--i];

                if (!kept.Contains(sp))
                {
                    spriteList.RemoveAt(i);
                }
            }

            atlas.SortAlphabetically();

            EditorUtility.SetDirty(atlas);
        }

        private static int Compare(SpriteEntry a, SpriteEntry b)
        {
            if (a == null && b != null){ return 1; }

            if (a != null && b == null){ return -1; }

            var aPixels = a.width * a.height;
            var bPixels = b.width * b.height;

            if (aPixels > bPixels){ return -1; }

            if (aPixels < bPixels){ return 1; }

            return 0;
        }
    }
}
