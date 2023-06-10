﻿
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using UniRx;

namespace Extensions.Devkit
{
    public static class DirectoryUtility
    {
        /// <summary>
        /// フォルダを再帰的に処理して含まれるファイルのパスをmetaファイルを除いて返します.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IEnumerable<string> ExpnadFiles(string path)
        {
            // 空文字は除外.
            if (string.IsNullOrEmpty(path)) yield break;

            // ディレクトリでない場合は、単一ファイルなので自身を返す.
            if (!Directory.Exists(path))
            {
                yield return path;
                yield break;
            }

            // サブディレクトリーに対して再帰的に処理.
            foreach (var directory in Directory.GetDirectories(path))
            {
                foreach (var file in ExpnadFiles(directory))
                {
                    yield return file;
                }
            }

            // 対象フォルダー内で.meta以外のファイルについて処理.
            foreach (var file in Directory.GetFiles(path).Where(x => !x.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)))
            {
                yield return file;
            }
        }
    }
}