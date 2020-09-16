
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Extensions;
using Extensions.Devkit;

namespace Modules.Devkit.TextureEdit
{
    public sealed class EditableTexture
    {
        //----- params -----

        private static readonly string[] PlatformNames = new string[] { "Standalone", "Android", "iPhone" };

        private sealed class TextureSetting
        {
            public bool readable;
            public TextureImporterCompression compression;
            
            #if !UNITY_5_5_OR_NEWER

            public TextureImporterFormat format;

            #endif
        }

        private sealed class PlatformSetting
        {
            #if UNITY_5_5_OR_NEWER

            public bool overridden;
            public TextureImporterFormat format;
            public TextureImporterCompression compression;
            public int compressionQuality;
            public bool allowsAlphaSplitting;

            #endif
        }

        //----- field -----

        private Texture texture = null;

        private TextureSetting textureSetting = null;
        private Dictionary<string, PlatformSetting> platformSettings = null;

        //----- property -----

        public Texture Texture { get { return texture; } }

        public string Guid { get { return UnityEditorUtility.GetAssetGUID(texture); } }

        //----- method -----

        public EditableTexture(string assetPath)
        {
            this.texture = AssetDatabase.LoadMainAssetAtPath(assetPath) as Texture;

            SaveSettings();
        }

        public EditableTexture(Texture texture)
        {
            this.texture = texture;

            SaveSettings();
        }

        public void Editable()
        {
            if(texture == null) { return; }

            var path = AssetDatabase.GetAssetPath(texture);

            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            if (textureImporter == null) { return; }

            var changed = false;

            // ------ TextureSetting ------

            changed |= ValueUpdate(textureImporter, true, "isReadable");
            changed |= ValueUpdate(textureImporter, TextureImporterCompression.Uncompressed, "textureCompression");

            #if !UNITY_5_5_OR_NEWER
            
            changed |= ValueUpdate(textureImporter, TextureImporterFormat.AutomaticTruecolor, "textureFormat");

            #endif

            // ------ PlatformSetting ------

            #if UNITY_5_5_OR_NEWER

            foreach (var platformName in PlatformNames)
            {
                var platformSettingsChanged = false;

                var platformTextureSetting = textureImporter.GetPlatformTextureSettings(platformName);

                if (platformTextureSetting != null)
                {
                    platformSettingsChanged |= ValueUpdate(platformTextureSetting, false, "allowsAlphaSplitting");
                    platformSettingsChanged |= ValueUpdate(platformTextureSetting, false, "overridden");
                    platformSettingsChanged |= ValueUpdate(platformTextureSetting, TextureImporterFormat.ARGB32, "format");
                    platformSettingsChanged |= ValueUpdate(platformTextureSetting, 50, "compressionQuality");
                    platformSettingsChanged |= ValueUpdate(platformTextureSetting, TextureImporterCompression.Uncompressed, "textureCompression");

                    if (platformSettingsChanged)
                    {
                        textureImporter.SetPlatformTextureSettings(platformTextureSetting);
                    }
                }

                changed |= platformSettingsChanged;
            }

            #endif

            if(changed)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        public void Restore()
        {
            if(texture == null) { return; }

            var path = AssetDatabase.GetAssetPath(texture);

            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            if (textureImporter == null) { return; }

            var changed = false;

            // ------ TextureSetting ------

            if (textureSetting != null)
            {
                changed |= ValueUpdate(textureImporter, textureSetting.readable, "isReadable");
                changed |= ValueUpdate(textureImporter, textureSetting.compression, "textureCompression");

                #if !UNITY_5_5_OR_NEWER

                changed |= ValueUpdate(textureImporter, textureSetting.format, "textureFormat");

                #endif
            }

            // ------ PlatformSetting ------

            if (platformSettings != null)
            {
                #if UNITY_5_5_OR_NEWER

                foreach (var item in platformSettings)
                {
                    var platformSettingsChanged = false;

                    var platformTextureSetting = textureImporter.GetPlatformTextureSettings(item.Key);

                    if (platformTextureSetting != null)
                    {
                        var platformSetting = item.Value;

                        platformSettingsChanged |= ValueUpdate(platformTextureSetting, platformSetting.allowsAlphaSplitting, "allowsAlphaSplitting");
                        platformSettingsChanged |= ValueUpdate(platformTextureSetting, platformSetting.overridden, "overridden");
                        platformSettingsChanged |= ValueUpdate(platformTextureSetting, platformSetting.format, "format");
                        platformSettingsChanged |= ValueUpdate(platformTextureSetting, platformSetting.compressionQuality, "compressionQuality");
                        platformSettingsChanged |= ValueUpdate(platformTextureSetting, platformSetting.compression, "textureCompression");

                        if (platformSettingsChanged)
                        {
                            textureImporter.SetPlatformTextureSettings(platformTextureSetting);
                        }
                    }

                    changed |= platformSettingsChanged;
                }

                #endif
            }

            if (changed)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            }
        }

        private void SaveSettings()
        {
            if(texture == null) { return; }

            var path = AssetDatabase.GetAssetPath(texture);

            var textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;

            if (textureImporter == null) { return; }

            // ------ TextureSetting ------

            textureSetting = new TextureSetting();

            textureSetting.readable = textureImporter.isReadable;
            textureSetting.compression = textureImporter.textureCompression;

            #if !UNITY_5_5_OR_NEWER

            textureSetting.format = textureImporter.textureFormat:

            #endif

            // ------ PlatformSetting ------

            #if UNITY_5_5_OR_NEWER

            platformSettings = new Dictionary<string, PlatformSetting>();

            foreach (var platformName in PlatformNames)
            {
                var platformTextureSetting = textureImporter.GetPlatformTextureSettings(platformName);

                if (platformTextureSetting != null)
                {
                    var platformSetting = new PlatformSetting();

                    platformSetting.allowsAlphaSplitting = platformTextureSetting.allowsAlphaSplitting;
                    platformSetting.overridden = platformTextureSetting.overridden;
                    platformSetting.format = platformTextureSetting.format;
                    platformSetting.compressionQuality = platformTextureSetting.compressionQuality;
                    platformSetting.compression = platformTextureSetting.textureCompression;

                    platformSettings.Add(platformName, platformSetting);
                }
            }

            #endif
        }

        private static bool ValueUpdate<T, TValue>(T target, TValue value, string propertyName) where TValue : IComparable
        {
            var currentValue = Reflection.GetPublicProperty<T, TValue>(target, propertyName);

            Reflection.SetPublicProperty(target, propertyName, value);

            return currentValue.CompareTo(value) != 0;
        }
    }
}
