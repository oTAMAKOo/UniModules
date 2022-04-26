
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Extensions;

namespace Modules.Master
{
	public sealed class RecordFileLoader : IRecordFileLoader
	{
		//----- params -----

		private const string IndexFileExtension = ".index";

		private const string RecordFileExtension = ".record";

		private const string RecordFolderName = "Records";

		//----- field -----

		private SerializationFileUtility.Format format = SerializationFileUtility.Format.Yaml;

		private Dictionary<string, string> indexFileTable = null;

		//----- property -----

		//----- method -----

		public RecordFileLoader(SerializationFileUtility.Format format)
		{
			this.format = format;

			var config = MasterConfig.Instance;

			var sourceDirectory = config.SourceDirectory;

			var indexFiles = Directory.GetFiles(sourceDirectory, "*" + IndexFileExtension, SearchOption.AllDirectories);
				
			indexFileTable = indexFiles.ToDictionary(
				x =>
					{
						var fileName = Path.GetFileNameWithoutExtension(x);

						return MasterManager.DeleteMasterSuffix(fileName).ToLower();
					},
				PathUtility.ConvertPathSeparator);
		}

		public string GetRecordFileDirectory(Type masterType)
		{
			var masterName = GetMasterName(masterType);

			return indexFileTable.GetValueOrDefault(masterName.ToLower());
		}

		public async Task<object[]> LoadAllRecords(string directory, Type masterType, Type recordType)
		{
			var indexFilePath = GetRecordFileDirectory(masterType);

			if (string.IsNullOrEmpty(indexFilePath))
			{
				var masterName = GetMasterName(masterType);

				Debug.LogErrorFormat("Master index file not found.\n MasterFileName : {0}", masterName);
			}

			var masterDataDirectory = Directory.GetParent(directory);

			var recordFileDirectory = PathUtility.Combine(masterDataDirectory.FullName, RecordFolderName);

			if (!Directory.Exists(recordFileDirectory)) { return null; }

			var recordFiles = Directory.GetFiles(recordFileDirectory, "*" + RecordFileExtension, SearchOption.TopDirectoryOnly);

			// 読み込み.

			var records = new SortedDictionary<string, object>(new NaturalComparer());

			var tasks = new List<Task>();

			foreach (var recordFile in recordFiles)
			{
				var filePath = recordFile;

				var task = Task.Run(() =>
				{
					var fileName = Path.GetFileNameWithoutExtension(filePath);

					var record = SerializationFileUtility.LoadFile(filePath, recordType, format);

					if (record != null)
					{
						lock (records)
						{
							records.Add(fileName, record);
						}
					}
				});

				tasks.Add(task);
			}

			await Task.WhenAll(tasks);

			return records.Values.ToArray();
		}

		private string GetMasterName(Type masterType)
		{
			var masterManager = MasterManager.Instance;

			var masterFileName = masterManager.GetMasterFileName(masterType, false);

			var masterName = Path.GetFileNameWithoutExtension(masterFileName);

			return masterName;
		}
	}
}