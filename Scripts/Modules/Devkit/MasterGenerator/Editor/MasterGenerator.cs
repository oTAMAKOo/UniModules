
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using Extensions;
using MessagePack;
using MessagePack.Resolvers;
using Modules.Devkit.Console;
using Modules.MessagePack;

namespace Modules.Master
{
	public interface IRecordDataLoader
	{
		UniTask<object[]> GetAllRecords(Type masterType, Type recordType);
	}

    public static class MasterGenerator
    {
        //----- params -----

        private const string ContainerClassName = "Container";

        private const string RecordClassName = "Record";

		public const string VersionFileName = "version.txt";

		private sealed class ResultInfo
		{
			public string fileName = null;
			public string hash = null;
			public double time = 0f;
		}

        //----- field -----

        //----- property -----

        //----- method -----

		public static async UniTask<bool> Generate(Type[] masterTypes, IRecordDataLoader recordDataLoader)
		{
			var config = MasterConfig.Instance;

			var lz4Compression = config.Lz4Compression;

			var sourceDirectory = config.SourceDirectory;

			if (string.IsNullOrEmpty(sourceDirectory)) { return false; }

			var exportDirectory = GetExportDirectory();

			if (string.IsNullOrEmpty(exportDirectory)){ return false; }

			if (Directory.Exists(exportDirectory))
			{
				DirectoryUtility.Clean(exportDirectory);
			}

            // 暗号化キー.

            AesCryptoKey cryptoKey = null;

            if (!string.IsNullOrEmpty(config.CryptoKey) && !string.IsNullOrEmpty(config.CryptoIv))
            {
                cryptoKey = new AesCryptoKey(config.CryptoKey, config.CryptoIv);
            }

            // 実行.

			try
            {
	            var masterManager = MasterManager.Instance;

                masterManager.SetCryptoKey(cryptoKey);

                var fileInfoDictionary = new SortedDictionary<string, (string, long)>(new NaturalComparer());

				var resultInfos = new List<ResultInfo>();

                foreach (var masterType in masterTypes)
                {
                    try
                    {
                        var sw = System.Diagnostics.Stopwatch.StartNew();

                        // ファイル名.

                        var masterFileName = masterManager.GetMasterFileName(masterType);

                        // マスターコンテナ型.

                        var containerTypeName = string.Format("{0}+{1}", masterType.FullName, ContainerClassName);

                        var containerType = masterType.Assembly.GetType(containerTypeName);

                        // マスターレコード型.

                        var recordTypeName = string.Format("{0}+{1}", masterType.FullName, RecordClassName);

                        var recordType = masterType.Assembly.GetType(recordTypeName);

                        // マスター読み込み.

                        var master = await LoadMasterData(recordDataLoader, masterType, containerType, recordType);

                        if (master == null)
                        {
                            throw new Exception($"Failed load master : {masterFileName}");
                        }

                        // MessagePackファイル作成.

                        var filePath = PathUtility.Combine(exportDirectory, masterFileName);

                        var fileName = Path.GetFileNameWithoutExtension(filePath);

                        var versionHash = await GenerateMasterFile(filePath, master,  cryptoKey, lz4Compression);

                        var file = new FileInfo(filePath);

                        // バージョンハッシュ.
                        fileInfoDictionary.Add(fileName, (versionHash, file.Length));

                        sw.Stop();

						var resultInfo = new ResultInfo()
						{
							fileName = masterType.FullName,
							time = sw.Elapsed.TotalMilliseconds,
							hash = versionHash,
						};

						resultInfos.Add(resultInfo);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"{masterType.FullName}\n\n{e.Message}");
                    }
                }

                // バージョンファイル作成.
				GenerateMasterVersionFile(exportDirectory, fileInfoDictionary);

				// ログ.

				var logBuilder = new StringBuilder();

				var chunkedResultInfos = resultInfos.Chunk(50).ToArray();

				var chunkNum = chunkedResultInfos.Length;

				for (var i = 0; i < chunkedResultInfos.Length; i++)
				{
					var items = chunkedResultInfos[i];

					logBuilder.Clear();

					logBuilder.AppendLine($"Generate master complete. [{i + 1}/{chunkNum}]");

					foreach (var item in items)
					{
						logBuilder.AppendFormat("{0} ({1:F2}ms)", item.fileName, item.time).AppendLine();
						logBuilder.AppendFormat("[ {0} ]", item.hash).AppendLine();
						logBuilder.AppendLine();
					}

					UnityConsole.Info(logBuilder.ToString());
				}
			}
            catch (Exception e)
            {
				var eventName = UnityConsole.InfoEvent.ConsoleEventName;
				var color = UnityConsole.InfoEvent.ConsoleEventColor;

                UnityConsole.Event(eventName, color, $"Generate master failed. \n\n{e.Message}", LogType.Error);

                Debug.LogException(e);

                return false;
            }

            return true;
        }

        public static string GetExportDirectory()
        {
            var config = MasterConfig.Instance;

            var exportDirectory = config.ExportDirectory;

            if (string.IsNullOrEmpty(exportDirectory)){ return null; }

            // 「Master」フォルダ追加.
            exportDirectory = PathUtility.Combine(exportDirectory, MasterManager.FolderName);

            return exportDirectory;
        }

        #region Load Data

        private static async UniTask<object> LoadMasterData(IRecordDataLoader recordDataLoader, Type masterType, Type containerType, Type recordType)
        {
	        // Load records.

            var records = await recordDataLoader.GetAllRecords(masterType, recordType);

            if (records == null) { return null; }

            // Create MessagePack binary.

            var container = BuildMasterContainer(containerType, recordType, records);

            return container;
        }

        private static object BuildMasterContainer(Type containerType, Type recordType, object[] records)
        {
            // コンテナ作成.
            var container = Activator.CreateInstance(containerType);

            // レコード情報.

            var flags = BindingFlags.GetField | BindingFlags.SetField | BindingFlags.Public | BindingFlags.Instance;

            var fieldInfo = Reflection.GetFieldInfo(containerType, "records", flags);

            // 配列化.

            var array = records.ToArray();

            var recordArray = Array.CreateInstance(recordType, array.Length);

            Array.Copy(array, recordArray, array.Length);

            // 設定.

            fieldInfo.SetValue(container, recordArray);

            return container;
        }

        #endregion

        #region Generate File

		private static async UniTask<string> GenerateMasterFile(string filePath, object master, AesCryptoKey dataCryptoKey, bool lz4Compression)
        {
            var options = StandardResolverAllowPrivate.Options.WithResolver(UnityCustomResolver.Instance);

            if (lz4Compression)
            {
                options = options.WithCompression(MessagePackCompression.Lz4BlockArray);
            }

            // 変換.

            var json = master.ToJson();

            var bytes = MessagePackSerializer.ConvertFromJson(json, options);

            // 暗号化.

            if (dataCryptoKey != null)
            {
                bytes = bytes.Encrypt(dataCryptoKey);
            }

            // ファイル出力.

            CreateFileDirectory(filePath);

            using (var file = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                await file.WriteAsync(bytes, 0, bytes.Length);
            }
            
            var fileHash = json.GetHash();

            return fileHash;
        }

        private static void CreateFileDirectory(string filePath)
        {
            var directory = Path.GetDirectoryName(filePath);

            if (Directory.Exists(directory)) { return; }

            Directory.CreateDirectory(directory);
        }

        #endregion

        #region Version

		private static void GenerateMasterVersionFile(string filePath, IDictionary<string, (string hash, long fileSize)> versionHashDictionary)
        {
            var builder = new StringBuilder();

			var rootHash =  string.Empty;

            foreach (var item in versionHashDictionary.Values)
            {
                rootHash += item.hash;
            }

            rootHash = rootHash.GetHash();

            if (string.IsNullOrEmpty(rootHash))
			{
				throw new InvalidDataException();
			}

			builder.AppendLine(rootHash);

			foreach (var item in versionHashDictionary)
            {
                builder.AppendFormat("{0},{1},{2}", item.Key, item.Value.hash, item.Value.fileSize).AppendLine();
            }
            
            var versionText = builder.ToString();

            var path = PathUtility.Combine(filePath, VersionFileName);

            using (var writer = new StreamWriter(path, false))
            {
                writer.Write(versionText);
            }
        }

        #endregion
    }
}
