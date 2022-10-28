
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extensions;

namespace Modules.Master
{
	public sealed class RecordDataLoader : IRecordDataLoader
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

		public RecordDataLoader(SerializationFileUtility.Format format)
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

		public async UniTask<object[]> GetAllRecords(Type masterType, Type recordType)
		{
			var masterManager = MasterManager.Instance;

            var config = MasterConfig.Instance;

            if (!string.IsNullOrEmpty(config.CryptoKey) && !string.IsNullOrEmpty(config.CryptoIv))
            {
                var cryptoKey = new AesCryptoKey(config.CryptoKey, config.CryptoIv);

                masterManager.SetCryptoKey(cryptoKey);
            }

            var masterFileName = masterManager.GetMasterFileName(masterType);

			var masterName = Path.GetFileNameWithoutExtension(masterFileName);

			var indexFilePath = indexFileTable.GetValueOrDefault(masterName.ToLower());

			if (string.IsNullOrEmpty(indexFilePath))
			{
				Debug.LogErrorFormat("Master index file not found.\n MasterFileName : {0}", masterName);

				return null;
			}

			var masterDataDirectory = Directory.GetParent(indexFilePath);

			var recordFileDirectory = PathUtility.Combine(masterDataDirectory.FullName, RecordFolderName);

			if (!Directory.Exists(recordFileDirectory)) { return null; }

			var recordFiles = Directory.GetFiles(recordFileDirectory, "*" + RecordFileExtension, SearchOption.TopDirectoryOnly);

			// 読み込み.

			var records = new SortedDictionary<string, object>(new NaturalComparer());

			var tasks = new List<UniTask>();

			foreach (var recordFile in recordFiles)
			{
				var filePath = recordFile;

				var task = UniTask.RunOnThreadPool(() =>
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

			await UniTask.WhenAll(tasks);

			return records.Values.ToArray();
		}
	}
}