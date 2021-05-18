﻿﻿
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Text;
using System.IO;
using Extensions;

namespace Modules.Devkit.Generators
{
	public static class ScriptGenerateUtility
    {
        //----- params -----

        // 無効な文字を管理する配列
        private static readonly string[] InvaludChars =
        {
            " ", "!", "\"", "#", "$",
            "%", "&", "\'", "(", ")",
            "-", "=", "^",  "~", "\\",
            "|", "[", "{",  "@", "`",
            "]", "}", ":",  "*", ";",
            "+", "/", "?",  ".", ">",
            ",", "<"
        };

        //----- field -----

        //----- property -----

        //----- method -----

        public static bool GenerateScript(string folderPath, string fileName, string script)
        {
            if (!AssetDatabase.IsValidFolder(folderPath)){ return false; }
            
            var folder = PathUtility.Combine(Application.dataPath, folderPath);
            
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var assetPath = PathUtility.Combine(folderPath, fileName);

            var fullPath = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);

            var requireUpdate = true;

            if (File.Exists(fullPath))
            {
                using (var sr = new StreamReader(fullPath, Encoding.UTF8))
                {
                    var text = sr.ReadToEnd();

                    requireUpdate = text != script;
                }
            }

            if (requireUpdate)
            {
                File.WriteAllText(fullPath, script, Encoding.UTF8);

                AssetDatabase.ImportAsset(assetPath);
            }

            return true;
        }

        /// <summary>
        /// C#で扱える名前に変換.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="checkFirstChar"></param>
        /// <returns></returns>
        public static string GetCSharpName(string name, bool checkFirstChar = true)
        {
            if (string.IsNullOrEmpty(name)) { return null; }

            var result = string.Empty;

            if (checkFirstChar)
            {
                // 先頭文字が数値の場合は「_ 」追加.
                var first = name.ToCharArray().First();

                if (char.IsNumber(first))
                {
                    name = '_' + name;
                }
            }

            // 使用出来ない文字は「_ 」に置き換え.
            foreach (var c in name.ToCharArray())
            {
                result += InvaludChars.Contains(c.ToString()) ? '_' : c;
            }

            return result;
        }

        /// <summary>
        /// 無効な文字を削除します
        /// </summary>
        public static string RemoveInvalidChars(string str)
        {
            if (string.IsNullOrEmpty(str)) { return str; }

            Array.ForEach(InvaludChars, c => str = str.Replace(c, string.Empty));
            return str;
        }
    }
}
