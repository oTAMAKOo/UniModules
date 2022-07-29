
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.Console;
using Modules.Devkit.Prefs;

namespace Modules.Lua.Text
{
    public static class LuaTextAssetUpdater
    {
        //----- params -----

		private const int CheckInterval = 3;

		public static class Prefs
		{
			public static bool autoUpdate
			{
				get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-autoUpdate", false); }
				set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-autoUpdate", value); }
			}
		}

		private sealed class UpdateInfo
		{
			public string workspace = null;
			public string excelPath = null;
		}

		//----- field -----

		private static List<UpdateInfo> updateInfos = null;

		private static bool isUpdating = false;

		private static DateTime? nextCheckTime = null;

		private static Enum prevLanguage = default;

		private static Dictionary<string, string> sourceFolderPathCache = null;
		private static Dictionary<string, string> destFolderPathCache = null;
		private static Dictionary<string, string> assetPathCache = null;

        //----- property -----

        //----- method -----

		static LuaTextAssetUpdater()
		{
			updateInfos = new List<UpdateInfo>();

			sourceFolderPathCache = new Dictionary<string, string>();
			destFolderPathCache = new Dictionary<string, string>();
			assetPathCache = new Dictionary<string, string>();
		}

		[DidReloadScripts]
        private static void DidReloadScripts()
        {
            EditorApplication.update += AutoUpdateLuaTextAssetCallback;
        }

        private static void AutoUpdateLuaTextAssetCallback()
        {
			if (Application.isPlaying) { return; }

			if (EditorApplication.isCompiling) { return; }

			if (EditorApplication.isUpdating){ return; }

			if (!Prefs.autoUpdate){ return; }

			if (isUpdating){ return; }

            if (nextCheckTime.HasValue)
            {
                if (DateTime.Now < nextCheckTime) { return; }
            }

			UpdateLuaText().Forget();
		}

		private static async UniTask UpdateLuaText()
        {
			var config = LuaTextConfig.Instance;

			if (config == null){ return; }

			var language = LuaTextLanguage.Instance.Current;

			if (language == null){ return; }

			if (prevLanguage != language.Language)
			{
				sourceFolderPathCache.Clear();
				destFolderPathCache.Clear();
				assetPathCache.Clear();

				prevLanguage = language.Language;
			}

			isUpdating = true;

			updateInfos.Clear();

			foreach (var info in config.TransferInfos)
			{
				var sourceFolder = sourceFolderPathCache.GetOrAdd(info.sourceFolderRelativePath, x => UnityPathUtility.RelativePathToFullPath(x));

				var destFolder = destFolderPathCache.GetOrAdd(info.destFolderGuid, x => AssetDatabase.GUIDToAssetPath(x));

				var excelPaths = LuaTextExcel.FindExcelFile(sourceFolder);

				foreach (var excelPath in excelPaths)
				{
					var assetPath = GetAssetPathFromExcelPath(excelPath, sourceFolder, destFolder, language);

					var requireUpdate = IsRequireUpdate(excelPath, assetPath);

					if (requireUpdate)
					{
						var updateInfo = new UpdateInfo()
						{
							workspace = sourceFolder,
							excelPath = excelPath,
						};

						updateInfos.Add(updateInfo);
					}
				}
			}

			if (updateInfos.Any())
			{
				var log = new StringBuilder();

				var groups = updateInfos.GroupBy(x => x.workspace).ToArray();

				foreach (var group in groups)
				{
					var workspace = group.Key;
					var targetExcels = group.Select(x => x.excelPath).ToArray();

					await LuaTextExcel.Export(workspace, targetExcels, false);
				}

				var excelPaths = updateInfos.Select(x => x.excelPath).ToArray();

				var bookDatas = await DataLoader.LoadBookData(excelPaths);

				if (bookDatas.Any())
				{
					// Nullのデータがある場合は生成スキップ.
					if (bookDatas.All(x => x.sheets.All(y => y != null)))
					{
						log.AppendLine("LuaText auto updated.").AppendLine();

						foreach (var bookData in bookDatas)
						{
							LuaTextAssetGenerator.Generate(bookData);

							var assetPath = LuaText.GetAssetFileName(bookData.DestPath, language.Identifier);

							log.AppendLine(assetPath);
						}

						UnityConsole.Info(log.ToString());
					}
				}
			}

			isUpdating = false;
			
			nextCheckTime = DateTime.Now.AddSeconds(CheckInterval);
		}

		private static bool IsRequireUpdate(string excelPath, string assetPath)
		{
			var luaTextAsset = AssetDatabase.LoadAssetAtPath<LuaTextAsset>(assetPath);

			// 存在しない.
			if (luaTextAsset == null){ return true; }

			// 更新時間が存在しない.
			if (!luaTextAsset.UpdateAt.HasValue){ return true; }

			// Excelの更新時間より古い.

			var excelFileInfo = new FileInfo(excelPath);

			var luaTextUpdateAt = luaTextAsset.UpdateAt.Value;
			var excelUpdateAt = excelFileInfo.LastWriteTime.ToUnixTime();

			return luaTextUpdateAt < excelUpdateAt;
		}

		private static string GetAssetPathFromExcelPath(string excelPath, string sourceFolder, string destFolder, LanguageInfo language)
		{
			var assetPath = string.Empty;

			if (assetPathCache.ContainsKey(excelPath))
			{
				assetPath = assetPathCache[excelPath];
			}
			else
			{
				var temp = excelPath.Replace(sourceFolder, destFolder);

				temp = PathUtility.GetPathWithoutExtension(temp);

				assetPath = LuaText.GetAssetFileName(temp, language.Identifier);

				assetPathCache[excelPath] = assetPath;
			}

			return assetPath;
		}
    }
}