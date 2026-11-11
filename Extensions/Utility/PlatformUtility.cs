
using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;

#endif

namespace Extensions
{
    public static class PlatformUtility
    {
        /// <summary> 現在のプラットホーム名を取得 </summary>
        public static string GetPlatformName()
        {
            #if UNITY_EDITOR

            var target = EditorUserBuildSettings.activeBuildTarget;

            switch (target)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                case BuildTarget.WebGL:
                    return "WebGL";
                default:
                    return null;
            }

            #else

            var platform = Application.platform;

            switch (platform)
            {
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return "Windows";
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "OSX";
                default:
                    return null;
            }

            #endif
        }

        /// <summary> 現在のプラットホームの種別名を取得 </summary>
        public static string GetPlatformTypeName()
        {
            var folderName = string.Empty;

            #if UNITY_EDITOR

            var target = EditorUserBuildSettings.activeBuildTarget;

            switch (target)
            {
                case BuildTarget.Android:
                    folderName = "Android";
                    break;

                case BuildTarget.iOS:
                    folderName = "iOS";
                    break;

                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                case BuildTarget.StandaloneOSX:
                    folderName = "Standalone";
                    break;

                case BuildTarget.WebGL:
                    folderName = "WebGL";
                    break;
            }

            #else

            var platform = Application.platform;

            switch (platform)
            {
                case RuntimePlatform.Android:
                    folderName = "Android";
                    break;

                case RuntimePlatform.IPhonePlayer:
                    folderName = "iOS";
                    break;

                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    folderName = "Standalone";
                    break;

                case RuntimePlatform.WebGLPlayer:
                    folderName = "WebGL";
                    break;
            }

            #endif

            return folderName;
        }
    }
}
