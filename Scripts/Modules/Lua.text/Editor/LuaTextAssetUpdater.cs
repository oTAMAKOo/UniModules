
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
using Extensions.Devkit;
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

		//----- field -----

		private static List<BookData> updateTargets = null;

		private static bool isUpdating = false;

		private static DateTime? nextCheckTime = null;

		private static Enum prevLanguage = default;

		private static Dictionary<string, long> excelLastUpdateAt = null;

		private static Dictionary<string, string> sourceFolderPathCache = null;
		private static Dictionary<string, string> destFolderPathCache = null;
		private static Dictionary<string, string> assetPathCache = null;

        //----- property -----

        //----- method -----

		static LuaTextAssetUpdater()
		{
			updateTargets = new List<BookData>();
			excelLastUpdateAt = new Dictionary<string, long>();

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

			//----- Export -----

			foreach (var info in config.TransferInfos)
			{
				var sourceFolder = sourceFolderPathCache.GetOrAdd(info.sourceFolderRelativePath, x => UnityPathUtility.RelativePathToFullPath(x));
				
				var excelPaths = LuaTextExcel.FindExcelFile(sourceFolder);

				var targetExcels = excelPaths.Where(x => IsExcelUpdated(x)).ToArray();

				if (targetExcels.Any())
				{
					await LuaTextExcel.Export(sourceFolder, targetExcels, false);
				}
			}
			
			//----- Build BookData -----

			updateTargets.Clear();
			
			foreach (var info in config.TransferInfos)
			{
				var sourceFolder = sourceFolderPathCache.GetOrAdd(info.sourceFolderRelativePath, x => UnityPathUtility.RelativePathToFullPath(x));
				
				var excelPaths = LuaTextExcel.FindExcelFile(sourceFolder);

				var bookDatas = await DataLoader.GetBookData(excelPaths);
				
				foreach (var excelPath in excelPaths)
				{
					var sourcePath = PathUtility.GetPathWithoutExtension(excelPath);

					var bookData = bookDatas.FirstOrDefault(x => x.SourcePath == sourcePath);

					if (bookData != null)
					{
						var requireUpdate = LuaTextAssetGenerator.IsRequireUpdate(bookData);

						if (requireUpdate)
						{
							updateTargets.Add(bookData);
						}
					}
					else
					{
						Debug.LogErrorFormat("BookData not found.\n{0}", excelPath);
					}
				}
			}

			//----- Update LuaText -----
			
			if (updateTargets.Any())
			{
				var log = new StringBuilder();

				var tasks = new List<UniTask>();

				log.AppendLine("LuaText auto updated.").AppendLine();
				
				foreach (var bookData in updateTargets)
				{
					var data = bookData;

					var task = UniTask.Create(async () =>
					{
						var sheetDatas = await DataLoader.LoadSheetData(data);

						LuaTextAssetGenerator.Generate(data, sheetDatas);

						var assetPath = LuaText.GetAssetFileName(data.DestPath, language.Identifier);

						lock (log)
						{
							log.AppendLine(assetPath);
						}

					});

					tasks.Add(task);
				}

				using (new AssetEditingScope())
				{
					await UniTask.WhenAll(tasks);
				}
				
				UnityConsole.Info(log.ToString());
			}

			isUpdating = false;
			
			nextCheckTime = DateTime.Now.AddSeconds(CheckInterval);
		}

		private static bool IsExcelUpdated(string excelPath)
		{
			if (!File.Exists(excelPath)){ return false; }

			var fileInfo = new FileInfo(excelPath);

			var hasValue = excelLastUpdateAt.ContainsKey(excelPath);

			var lastUpdateAt = excelLastUpdateAt.GetValueOrDefault(excelPath);
			var lastWriteTime = fileInfo.LastWriteTime.ToUnixTime();

			excelLastUpdateAt[excelPath] = lastWriteTime;

			return hasValue && lastUpdateAt < lastWriteTime;
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