﻿
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

        private const string ExportFolderName = "Masters";

        public const string VersionFileName = "version.txt";

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

            var logBuilder = new StringBuilder();

            try
            {
	            var masterManager = MasterManager.Instance;

                var fileInfoDictionary = new SortedDictionary<string, (string, long)>(new NaturalComparer());

                var tasks = new List<UniTask>();

                foreach (var masterType in masterTypes)
                {
                    var task = UniTask.RunOnThreadPool(async () =>
                    {
                        try
                        {
                            var sw = System.Diagnostics.Stopwatch.StartNew();

							// ファイル名.

							var masterFileName = masterManager.GetMasterFileName(masterType, false);

                            // マスターコンテナ型.

                            var containerTypeName = string.Format("{0}+{1}", masterType.FullName, ContainerClassName);

                            var containerType = masterType.Assembly.GetType(containerTypeName);

                            // マスターレコード型.

                            var recordTypeName = string.Format("{0}+{1}", masterType.FullName, RecordClassName);

                            var recordType = masterType.Assembly.GetType(recordTypeName);

                            // マスター読み込み.

                            var master = await LoadMasterData(recordDataLoader, masterType, containerType, recordType);

                            // MessagePackファイル作成.

                            var filePath = GetGenerateMasterFilePath(exportDirectory, masterFileName, cryptoKey);

                            var fileName = Path.GetFileNameWithoutExtension(filePath);

                            var versionHash = await GenerateMasterFile(filePath, master,  cryptoKey, lz4Compression);

							var file = new FileInfo(filePath);

							// バージョンハッシュ.

                            lock (fileInfoDictionary)
                            {
								fileInfoDictionary.Add(fileName, (versionHash, file.Length));
                            }

                            sw.Stop();

                            lock (logBuilder)
                            {
                                logBuilder.AppendFormat("{0} ({1:F2}ms)", masterType.FullName, sw.Elapsed.TotalMilliseconds).AppendLine();
                                logBuilder.AppendFormat("[ {0} ]", versionHash).AppendLine();
                                logBuilder.AppendLine();
                            }
                        }
                        catch (Exception e)
                        {
                            lock (logBuilder)
                            {
                                logBuilder.AppendLine();
                                logBuilder.AppendFormat("Error: {0}", masterType.FullName).AppendLine();
                                logBuilder.Append(e.Message).AppendLine();
                                logBuilder.AppendLine();
                            }

                            throw;
                        }
                    });

                    tasks.Add(task);
                }

                await UniTask.WhenAll(tasks);

                // バージョンファイル作成.
				GenerateMasterVersionFile(exportDirectory, fileInfoDictionary);

                UnityConsole.Info("Generate master complete.\n\n{0}", logBuilder.ToString());
            }
            catch (Exception e)
            {
                UnityConsole.Info("Generate master failed. \n\n{0}", logBuilder.ToString());

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
            exportDirectory = PathUtility.Combine(exportDirectory, ExportFolderName);

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

        private static string GetGenerateMasterFilePath(string exportPath, string masterFileName, AesCryptoKey fileNameCryptoKey)
        {
            if (fileNameCryptoKey != null)
            {
                masterFileName = masterFileName.Encrypt(fileNameCryptoKey, true);
            }

            var filePath = PathUtility.Combine(exportPath, masterFileName);

            return filePath;
        }

        private static async UniTask<string> GenerateMasterFile(string filePath, object master, AesCryptoKey dataCryptoKey, bool lz4Compression)
        {
            var options = StandardResolverAllowPrivate.Options.WithResolver(UnityContractResolver.Instance);

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

			var rootHash =  string.Join(null, versionHashDictionary.Values).GetHash();

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
