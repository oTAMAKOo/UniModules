
using UnityEditor;

namespace Modules.SecureStorage
{
    /// <summary>
    /// ENABLE_SECURE_STORAGE が定義されているときだけ、ネイティブファイル（.mm / .java）をビルドに含める.
    /// 判定はこのスクリプトのコンパイル時のシンボルで決まるため、csc.rsp の定義も使える.
    /// </summary>
    public static class SecureStoragePluginFilter
    {
        //----- params -----

        private static readonly string[] PluginGuids = new string[]
        {
            // Plugins/iOS/SecureStorage.mm.
            "2bfc551a35b7299488910364ff1e5246",

            // Plugins/Android/SecureStorage.java.
            "054aec2076fb9724d95b030b4a69dd71",
        };

        //----- field -----

        //----- property -----

        public static bool Enable
        {
            get
            {
                #if ENABLE_SECURE_STORAGE

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
