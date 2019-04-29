﻿
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using Extensions.Devkit;
using Modules.Devkit.TextureEdit;

namespace Modules.SpriteSheet
{
    public partial class SpriteSheetMaker
    {
        //----- params -----

        private class SpriteEntry : SpriteData
        {
            public Texture2D texture;
            public bool temporaryTexture = false;

            public void SetTexture(Color32[] newPixels, int newWidth, int newHeight)
            {
                Release();

                temporaryTexture = true;

                texture = new Texture2D(newWidth, newHeight);
                texture.name = name;
                texture.SetPixels32(newPixels);
                texture.Apply();
            }

            public void Release()
            {
                if (temporaryTexture)
                {
                    Object.DestroyImmediate(texture);

                    texture = null;
                    temporaryTexture = false;
                }
            }
        }

        //----- field -----

        private Dictionary<string, EditableTexture> editableTextures = null;

        //----- property -----

        //----- method -----

        private void StartTextureEdit(SpriteSheet spriteSheet, List<Texture> textures)
        {
            AssetDatabase.StartAssetEditing();

            if (editableTextures == null)
            {
                editableTextures = new Dictionary<string, EditableTexture>();
            }
            else
            {
                foreach (var target in editableTextures.Values)
                {
                    target.Restore();
                }

                editableTextures.Clear();
            }

            if (spriteSheet != null)
            {
                var texture = spriteSheet.Texture as Texture2D;

                RegisterEditableTexture(texture, true);
            }

            foreach (var texture in textures)
            {
                RegisterEditableTexture(texture, false);
            }

            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        private void FinishTextureEdit()
        {
            AssetDatabase.StartAssetEditing();

            foreach (var editableTexture in editableTextures.Values)
            {
                editableTexture.Restore();
			}

            AssetDatabase.StopAssetEditing();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }

        private void RegisterEditableTexture(Texture texture, bool restore)
        {
            if(texture == null) { return; }

            var editableTexture = new EditableTexture(texture);

            if (!editableTextures.ContainsKey(editableTexture.Guid))
            {
                editableTexture.Editable();

				if (restore)
                {
                    editableTextures.Add(editableTexture.Guid, editableTexture);
                }
			}
		}

        private void UpdateSpriteSheet(SpriteSheet spriteSheet, List<Texture> textures, bool keepSprites)
        {
            if (spriteSheet == null) { return; }

            Texture2D texture = null;

            texture = spriteSheet.Texture as Texture2D;

            var sprites = CreateSprites(textures);

            if (sprites.Count > 0)
            {
                if (keepSprites)
                {
                    ExtractSprites(spriteSheet, sprites);
                }

                UpdateSpriteSheet(spriteSheet, sprites);
            }
            else if (!keepSprites)
            {
                UpdateSpriteSheet(spriteSheet, sprites);
            }

            texture = spriteSheet.Texture as Texture2D;

            if (texture != null)
            {
                SaveTextureMetaData(texture, sprites);
            }
        }

        private void UpdateSpriteSheet(SpriteSheet spriteSheet, List<SpriteEntry> sprites)
        {
            if (spriteSheet == null) { return; }

            if (sprites.Count > 0)
            {
                if (UpdateTexture(spriteSheet, sprites))
                {
                    ReplaceSprites(spriteSheet, sprites);
                }

                ReleaseSprites(sprites);

                spriteSheet.Padding = padding;
                spriteSheet.PixelsPerUnit = pixelsPerUnit;
                spriteSheet.FilterMode = filterMode;
            }
            else
            {
                spriteSheet.Sprites.Clear();

                var path = GetSaveableTexturePath(spriteSheet);
                
                spriteSheet.Padding = 0;
                spriteSheet.PixelsPerUnit = 100f;
                spriteSheet.FilterMode = FilterMode.Bilinear;

                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private List<SpriteEntry> CreateSprites(List<Texture> textures)
        {
            var list = new List<SpriteEntry>();

            foreach (var item in textures)
            {
                var texture = item as Texture2D;

                if (texture == null) { continue; }

                var sprite = new SpriteEntry();

                sprite.SetRect(0, 0, texture.width, texture.height);
                sprite.texture = texture;
                sprite.name = texture.name;
                sprite.guid = UnityEditorUtility.GetAssetGUID(texture);
                sprite.temporaryTexture = false;

                list.Add(sprite);
            }

            return list;
        }

        private List<SpriteEntry> ExtractAllSprite(SpriteSheet spriteSheet)
        {
            var sprites = new List<SpriteEntry>();

            if (spriteSheet.Texture == null) { return null; }

            foreach (var sd in spriteSheet.Sprites)
            {
                var identifier = string.IsNullOrEmpty(sd.guid) ? sd.name : sd.guid;

                var se = ExtractSprite(spriteSheet, identifier);

                if (se != null)
                {
                    sprites.Add(se);
                }
            }

            return sprites;
        }

        private SpriteEntry ExtractSprite(SpriteSheet spriteSheet, string identifier)
        {
            if (spriteSheet.Texture == null) { return null; }

            var sd = spriteSheet.GetSpriteData(identifier);

            if (sd == null) { return null; }

            var se = ExtractSprite(sd, spriteSheet.Texture as Texture2D);

            return se;
        }

        private SpriteEntry ExtractSprite(SpriteData es, Texture2D tex)
        {
            return (tex != null) ? ExtractSprite(es, tex.GetPixels32(), tex.width, tex.height) : null;
        }

        private SpriteEntry ExtractSprite(SpriteData es, Color32[] oldPixels, int oldWidth, int oldHeight)
        {
            var xmin = Mathf.Clamp(es.x, 0, oldWidth);
            var ymin = Mathf.Clamp(es.y, 0, oldHeight);
            var xmax = Mathf.Min(xmin + es.width, oldWidth - 1);
            var ymax = Mathf.Min(ymin + es.height, oldHeight - 1);
            var newWidth = Mathf.Clamp(es.width, 0, oldWidth);
            var newHeight = Mathf.Clamp(es.height, 0, oldHeight);

            if (newWidth == 0 || newHeight == 0) return null;

            var newPixels = new Color32[newWidth * newHeight];

            for (int y = 0; y < newHeight; ++y)
            {
                int cy = ymin + y;
                if (cy > ymax) cy = ymax;

                for (int x = 0; x < newWidth; ++x)
                {
                    int cx = xmin + x;
                    if (cx > xmax) cx = xmax;

                    int newIndex = (newHeight - 1 - y) * newWidth + x;
                    int oldIndex = (oldHeight - 1 - cy) * oldWidth + cx;

                    newPixels[newIndex] = oldPixels[oldIndex];
                }
            }

            var sprite = new SpriteEntry();

            sprite.CopyFrom(es);
            sprite.SetRect(0, 0, newWidth, newHeight);
            sprite.SetTexture(newPixels, newWidth, newHeight);

            return sprite;
        }
    }
}
