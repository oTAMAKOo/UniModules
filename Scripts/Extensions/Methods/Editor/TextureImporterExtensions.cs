
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;

namespace Extensions
{
    public static class TextureImporterExtensions
    {
        private static MethodInfo getWidthAndHeightMethodInfo = null;

        /// <summary> テクスチャの警告取得 </summary>
        public static string GetImportWarning(this TextureImporter importer)
        {
            if (importer == null){ return null; }

            // ※ GetImportWarningsはinternalなのでリフレクションで呼び出す.
            var warning = Reflection.InvokePrivateMethod(importer, "GetImportWarnings") as string;

            return warning;
        }

		/// <summary> テクスチャサイズ取得 </summary>
        public static Vector2 GetPreImportTextureSize(this TextureImporter importer)
        {
            // ※ TextureImporter.GetWidthAndHeightは非公開メソッドなので無理矢理呼び出し.     

            var args = new object[] { 0, 0 };

            if (getWidthAndHeightMethodInfo == null)
            {
                getWidthAndHeightMethodInfo = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            
            getWidthAndHeightMethodInfo.Invoke(importer, args);

            return new Vector2((int)args[0], (int)args[1]);
        }
    }
}
