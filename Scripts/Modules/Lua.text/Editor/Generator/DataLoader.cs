
using System;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.Lua.Text
{
    public static class DataLoader
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

		public static async UniTask<BookData[]> GetAllBookData()
		{
			return await GetBookDataInternal();
		}

		public static async UniTask<BookData[]> GetBookData(string[] excelPaths)
		{
			if (excelPaths == null || excelPaths.IsEmpty()){ return new BookData[0]; }

			var targets = excelPaths.Select(x => PathUtility.GetPathWithoutExtension(x) + ".").ToArray();

			return await GetBookDataInternal(x => targets.Any(y => x.StartsWith(y)));
		}

		private static async UniTask<BookData[]> GetBookDataInternal(Func<string, bool> filter = null)
		{
			var config = LuaTextConfig.Instance;

			var list = new List<BookData>();

			var transferInfos = config.TransferInfos.ToArray();

			foreach (var transferInfo in transferInfos)
			{
				var sourceFolderPath = UnityPathUtility.RelativePathToFullPath(transferInfo.sourceFolderRelativePath);
				var destFolderAssetPath = AssetDatabase.GUIDToAssetPath(transferInfo.destFolderGuid);

				if (string.IsNullOrEmpty(sourceFolderPath) || string.IsNullOrEmpty(destFolderAssetPath))				{ continue; }

				var sheetFilePaths = GetSheetFilePaths(sourceFolderPath, filter);

				var bookDatas = await GetBookDataFromSheetFiles(sheetFilePaths);

				foreach (var bookData in bookDatas)
				{
					var destDirectory = bookData.sourceDirectory.Replace(sourceFolderPath, destFolderAssetPath);

					bookData.destDirectory = destDirectory;
				}

				list.AddRange(bookDatas);

				await UniTask.Delay(1);
			}

			return list.ToArray();
		}

		private static string[] GetSheetFilePaths(string sourceFolderPath, Func<string, bool> filter = null)
		{
			var config = LuaTextConfig.Instance;

			var extension = FileLoader.GetFileExtension(config.Format);

			var sheetFilePaths = Directory.EnumerateFiles(sourceFolderPath, "*.*", SearchOption.AllDirectories)
				.Where(x => Path.GetExtension(x) == extension)
				.Select(x => PathUtility.ConvertPathSeparator(x))
				.Where(x => filter == null || filter(x))
				.ToArray();

			return sheetFilePaths;
		}

		private static async UniTask<BookData[]> GetBookDataFromSheetFiles(string[] sheetPaths)
		{
			var bookDatas = new List<BookData>();

			var sheetList = new Dictionary<string, List<string>>();

			var count = 0;
			
            // 同じ階層にあるブック名定義が同一のシートを纏めて1つのブックにする.

            foreach (var filePath in sheetPaths)
            {
				var sourceDirectory = Path.GetDirectoryName(filePath);

				sourceDirectory = PathUtility.ConvertPathSeparator(sourceDirectory);
                
				var fileName = Path.GetFileNameWithoutExtension(filePath);

				var bookName = fileName.Split('.').FirstOrDefault();
				
				var bookData = bookDatas.FirstOrDefault(x => x.sourceDirectory == sourceDirectory && x.bookName == bookName);

				if (bookData == null)
				{
					bookData = new BookData()
					{
						bookName = bookName,
						sourceDirectory = sourceDirectory,
					};

					sheetList.Add(bookData.SourcePath, new List<string>());

					bookDatas.Add(bookData);
				}

                var list = sheetList.GetValueOrDefault(bookData.SourcePath);

                if (!list.Contains(filePath))
                {
                    list.Add(filePath);
                }

				if(150 < count++)
				{
					count = 0;
					await UniTask.Delay(1);
				}
			}

			count = 0;

			var hashBuilder = new StringBuilder();

			foreach (var item in sheetList)
			{
				var bookData = bookDatas.FirstOrDefault(x => x.SourcePath == item.Key);

				bookData.sheets = item.Value.ToArray();

				hashBuilder.Clear();

				foreach (var sheet in bookData.sheets)
				{
					var hash = FileUtility.GetHash(sheet);

					hashBuilder.AppendLine(hash);
				}

				bookData.hash = hashBuilder.ToString().GetHash();

				if (150 < count++)
				{
					count = 0;
					await UniTask.Delay(1);
				}
			}

			return bookDatas.ToArray();
		}

		public static async UniTask<SheetData[]> LoadSheetData(BookData bookData)
		{
			var config = LuaTextConfig.Instance;
			
			var list = new List<SheetData>();

            var tasks = new List<UniTask>();

            foreach (var sheet in bookData.sheets)
			{
                var task = UniTask.RunOnThreadPool(() =>
                {
					var sheetData = FileLoader.LoadFile<SheetData>(sheet, config.Format);

					lock (list)
					{
						list.Add(sheetData);
					}
				});

				tasks.Add(task);
			}

            await UniTask.WhenAll(tasks);

			return list.ToArray();
		}
	}
}