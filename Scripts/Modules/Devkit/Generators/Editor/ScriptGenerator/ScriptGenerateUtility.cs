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
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                var path = PathUtility.Combine(folderPath, fileName);
                File.WriteAllText(path, script, Encoding.UTF8);
                AssetDatabase.ImportAsset(path);

                return true;
            }
            else
            {
                var path = PathUtility.Combine(Application.dataPath, folderPath);
                EditorUtility.DisplayDialog("Folder NotFound", string.Format("生成先のフォルダが存在しません\n{0}", path), "閉じる");
            }

            return false;
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
