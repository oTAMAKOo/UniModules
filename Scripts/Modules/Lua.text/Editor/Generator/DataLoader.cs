
using System;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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

		public static async UniTask<BookData[]> LoadBookAllData()
		{
			return await LoadBookDataInternal();
		}

		public static async UniTask<BookData[]> LoadBookData(string[] excelPaths)
		{
			if (excelPaths == null || excelPaths.IsEmpty()){ return new BookData[0]; }

			var targets = excelPaths.Select(x => PathUtility.GetPathWithoutExtension(x) + ".").ToArray();

			return await LoadBookDataInternal(x => targets.Any(y => x.StartsWith(y)));
		}

		private static async UniTask<BookData[]> LoadBookDataInternal(Func<string, bool> filter = null)
		{
			var config = LuaTextConfig.Instance;

			var extension = FileLoader.GetFileExtension(config.Format);

			var list = new List<BookData>();

			var transferInfos = config.TransferInfos.ToArray();

			foreach (var transferInfo in transferInfos)
			{
				var sourceFolderPath = UnityPathUtility.RelativePathToFullPath(transferInfo.sourceFolderRelativePath);
				var destFolderAssetPath = AssetDatabase.GUIDToAssetPath(transferInfo.destFolderGuid);

				if (string.IsNullOrEmpty(sourceFolderPath) || string.IsNullOrEmpty(destFolderAssetPath)){ continue; }

				var sheetFilePaths = Directory.EnumerateFiles(sourceFolderPath, "*.*", SearchOption.AllDirectories)
					.Where(x => Path.GetExtension(x) == extension)
					.Select(x => PathUtility.ConvertPathSeparator(x))
					.Where(x => filter == null || filter(x))
					.ToArray();

				var bookDatas = await LoadBookDataFromSheetFiles(sheetFilePaths);

				foreach (var bookData in bookDatas)
				{
					var destDirectory = bookData.sourceDirectory.Replace(sourceFolderPath, destFolderAssetPath);

					bookData.destDirectory = destDirectory;
				}

				list.AddRange(bookDatas);
			}

			return list.ToArray();
		}

		private static async UniTask<BookData[]> LoadBookDataFromSheetFiles(string[] sheetPaths)
		{
			var config = LuaTextConfig.Instance;

			var bookDatas = new List<BookData>();

			var sheetDataDictionary = new Dictionary<string, List<SheetData>>();

            var tasks = new List<UniTask>();

            // 同じ階層にあるブック名定義が同一のシートを纏めて1つのブックにする.

            foreach (var item in sheetPaths)
            {
                var filePath = item;

                var task = UniTask.RunOnThreadPool(() =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
					
                    var sourceDirectory = Path.GetDirectoryName(filePath);

					sourceDirectory = PathUtility.ConvertPathSeparator(sourceDirectory);
                    
                    var parts = fileName.Split('.').ToArray();

                    var bookName = parts.FirstOrDefault();

                    BookData bookData = null;

                    lock (bookDatas)
                    {
                        bookData = bookDatas.FirstOrDefault(x => x.sourceDirectory == sourceDirectory && x.bookName == bookName);

                        if (bookData == null)
                        {
                            bookData = new BookData()
                            {
                                bookName = bookName,
                                sourceDirectory = sourceDirectory,
                            };

                            sheetDataDictionary.Add(bookData.SourcePath, new List<SheetData>());

                            bookDatas.Add(bookData);
                        }
                    }

					var sheetName = 1 < parts.Length ? parts[1] : bookName;

                    var list = sheetDataDictionary.GetValueOrDefault(bookData.SourcePath);

                    if (list.All(x => x.sheetName != sheetName))
                    {
                        var sheetData = FileLoader.LoadFile<SheetData>(filePath, config.Format);

                        list.Add(sheetData);
                    }
				});
                
                tasks.Add(task);
            }

            await UniTask.WhenAll(tasks);

            foreach (var bookData in bookDatas)
            {
				var list = sheetDataDictionary.GetValueOrDefault(bookData.SourcePath);

                bookData.sheets = list.ToArray();
            }
			
            return bookDatas.ToArray();
		}
	}
}