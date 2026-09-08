
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using Extensions;

namespace Modules.Automation.PrefabPreview
{
    /// <summary> Prefabをプレビューシーンで描画してPNGへ書き出す </summary>
    public static class PrefabPreviewRenderer
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        /// <summary> アセットパス指定でPrefabを描画しPNGへ書き出す. Prefabが見つからない場合はnull </summary>
        public static PrefabPreviewResult Render(string prefabAssetPath, string outputFilePath, PrefabPreviewOptions options = null)
        {
            if (string.IsNullOrEmpty(prefabAssetPath)) { throw new ArgumentException("prefabAssetPath is empty."); }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);

            if (prefab == null)
            {
                Debug.LogError($"Prefab not found. ({prefabAssetPath}).");

                return null;
            }

            return Render(prefab, outputFilePath, options);
        }

        /// <summary> Prefabを描画しPNGへ書き出す </summary>
        public static PrefabPreviewResult Render(GameObject prefab, string outputFilePath, PrefabPreviewOptions options = null)
        {
            if (prefab == null) { throw new ArgumentNullException(nameof(prefab)); }

            if (string.IsNullOrEmpty(outputFilePath)) { throw new ArgumentException("outputFilePath is empty."); }

            if (options == null) { options = new PrefabPreviewOptions(); }

            Texture2D texture = null;

            try
            {
                using (var scope = new PrefabPreviewScope(prefab, options))
                {
                    texture = scope.Render();
                }

                var directory = Path.GetDirectoryName(outputFilePath);

                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(outputFilePath, texture.EncodeToPNG());
            }
            finally
            {
                if (texture != null)
                {
                    UnityUtility.SafeDelete(texture, true);
                }
            }

            var assetPath = AssetDatabase.GetAssetPath(prefab);

            return new PrefabPreviewResult(assetPath, outputFilePath, options.Width, options.Height);
        }
    }
}
