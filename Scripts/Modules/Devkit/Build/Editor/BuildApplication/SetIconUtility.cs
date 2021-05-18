
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;

namespace Modules.Devkit.Build
{
    public static class SetIconUtility
    {
        private static void SetDefaultIcon(string iconAssetDirectory, string iconFileName)
        {
            var iconFolderPath = PathUtility.Combine(iconAssetDirectory, "Icon");

            var iconTexture = LoadIconTextures(iconFolderPath, new string[] { iconFileName });

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, iconTexture);
        }

        public static void SetiOSPlatformIcons(string iconAssetDirectory)
        {
            var iconFolderPath = PathUtility.Combine(iconAssetDirectory, "iOS/Icon");

            SetPlatformIcon(BuildTargetGroup.iOS, UnityEditor.iOS.iOSPlatformIconKind.Application, iconFolderPath);
        }

        public static void SetAndroidPlatformIcons(string iconAssetDirectory)
        {
            //------ Adaptive (Background) ------
            {
                var iconFolderPath = PathUtility.Combine(iconAssetDirectory, "Android/Icon/Adaptive/Background");

                SetPlatformIcon(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, iconFolderPath, 0);
            }

            //------ Adaptive (Foreground) ------
            {
                var iconFolderPath = PathUtility.Combine(iconAssetDirectory, "Android/Icon/Adaptive/Foreground");

                SetPlatformIcon(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, iconFolderPath, 1);
            }

            //------ Round ------
            {
                var iconFolderPath = PathUtility.Combine(iconAssetDirectory, "Android/Icon/Round");

                SetPlatformIcon(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Round, iconFolderPath);
            }
        }

        public static void SetPlatformIcon(BuildTargetGroup platform, PlatformIconKind kind, string folderPath, int layer = 0)
        {
            var iconNames = new List<string>();

            var icons = PlayerSettings.GetPlatformIcons(platform, kind);

            var iconSizes = icons.Select(x => Tuple.Create(x.width, x.height)).ToArray();

            foreach (var iconSize in iconSizes)
            {
                var fileName = string.Format("{0}x{1}.png", iconSize.Item1, iconSize.Item2);

                iconNames.Add(fileName);
            }

            var iconTextures = LoadIconTextures(folderPath, iconNames);

            foreach (var icon in icons)
            {
                var iconTexture = iconTextures.FirstOrDefault(x => x.width == icon.width && x.height == icon.height);

                if (iconTexture != null)
                {
                    icon.SetTexture(iconTexture, layer);
                }
            }

            PlayerSettings.SetPlatformIcons(platform, kind, icons);
        }

        private static Texture2D[] LoadIconTextures(string folderPath, IEnumerable<string> iconNames)
        {
            var iconTextures = new List<Texture2D>();

            foreach (var iconName in iconNames)
            {
                var assetPath = PathUtility.Combine(folderPath, iconName);

                var texture = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D)) as Texture2D;

                if (texture != null)
                {
                    iconTextures.Add(texture);
                }
                else
                {
                    using (new DisableStackTraceScope())
                    {
                        Debug.LogErrorFormat("Icon texture not found. \n{0}\n", assetPath);
                    }
                }
            }

            return iconTextures.ToArray();
        }
    }
}
