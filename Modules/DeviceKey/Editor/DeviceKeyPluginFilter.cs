
using UnityEditor;

namespace Modules.DeviceKey
{
    /// <summary>
    /// ENABLE_DEVICE_KEY が定義されているときだけ、ネイティブファイル（.mm / .java）をビルドに含める.
    /// 判定はこのスクリプトのコンパイル時のシンボルで決まるため、csc.rsp の定義も使える.
    /// </summary>
    public static class DeviceKeyPluginFilter
    {
        //----- params -----

        private static readonly string[] PluginGuids = new string[]
        {
            // Plugins/iOS/DeviceKey.mm.
            "e50a6160aa727034baba98b5d4079257",

            // Plugins/Android/DeviceKey.java.
            "8523a9fc4e888034db1c42baa816270e",
        };

        //----- field -----

        //----- property -----

        public static bool Enable
        {
            get
            {
                #if ENABLE_DEVICE_KEY

                return true;

                #else

                return false;

                #endif
            }
        }

        //----- method -----

        [InitializeOnLoadMethod]
        private static void InitializeOnLoadMethod()
        {
            // 判定はアセットのパスに登録されるため、スクリプトの読み込みごとに登録し直す.
            if (Register()){ return; }

            // アセットの読み込みが終わっていないときは、エディタの更新後に登録する.
            EditorApplication.delayCall += () => Register();
        }

        private static bool Register()
        {
            var result = true;

            foreach (var guid in PluginGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);

                var importer = string.IsNullOrEmpty(assetPath) ? null : AssetImporter.GetAtPath(assetPath) as PluginImporter;

                if (importer == null)
                {
                    result = false;
                    continue;
                }

                importer.SetIncludeInBuildDelegate(IncludeInBuild);
            }

            return result;
        }

        private static bool IncludeInBuild(string assetPath)
        {
            return Enable;
        }
    }
}
