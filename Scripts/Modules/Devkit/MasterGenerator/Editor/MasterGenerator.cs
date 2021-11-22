
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Extensions;
using MessagePack;
using MessagePack.Resolvers;
using Modules.Devkit.Console;
using Modules.MessagePack;

namespace Modules.Master
{
    public static class MasterGenerator
    {
        //----- params -----

        private const string IndexFileExtension = ".index";

        private const string RecordFileExtension = ".record";

        private const string RecordFolderName = "Records";

        private const string ContainerClassName = "Container";

        private const string RecordClassName = "Record";

        private const string ExportFolderName = "Masters";

        public const string VersionFileName = "version.txt";

        //----- field -----

        //----- property -----

        //----- method -----

        public static async Task<bool> Generate(Type[] masterTypes)
        {
            var masterManager = MasterManager.Instance;

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

            // ファイル取得.

            var indexFiles = Directory.GetFiles(sourceDirectory, "*" + IndexFileExtension, SearchOption.AllDirectories);

            var indexFileTable = indexFiles.ToDictionary(
                x =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(x);
                    
                    return MasterManager.DeleteMasterSuffix(fileName).ToLower();
                },
                x =>
                {
                    return PathUtility.ConvertPathSeparator(x);
                });

            // 暗号化キー.

            AesCryptoKey dataCryptoKey = null;

            if (!string.IsNullOrEmpty(config.DataCryptKey) && !string.IsNullOrEmpty(config.DataCryptIv))
            {
                dataCryptoKey = new AesCryptoKey(config.DataCryptKey, config.DataCryptIv);
            }

            AesCryptoKey fileNameCryptoKey = null;

            if (!string.IsNullOrEmpty(config.FileNameCryptKey) && !string.IsNullOrEmpty(config.FileNameCryptIv))
            {
                fileNameCryptoKey = new AesCryptoKey(config.FileNameCryptKey, config.FileNameCryptIv);
            }

            // 実行.

            var logBuilder = new StringBuilder();

            try
            {
                var fileHashDictionary = new SortedDictionary<string, string>(new NaturalComparer());

                var tasks = new List<Task>();

                foreach (var masterType in masterTypes)
                {
                    var masterFileName = masterManager.GetMasterFileName(masterType, false);

                    var masterName = Path.GetFileNameWithoutExtension(masterFileName);

                    var indexFilePath = indexFileTable.GetValueOrDefault(masterName.ToLower());

                    if (string.IsNullOrEmpty(indexFilePath))
                    {
                        Debug.LogErrorFormat("Master index file not found.\n MasterFileName : {0}", masterName);
                        continue;
                    }
                    
                    var task = Task.Run(async () =>
                    {
                        try
                        {
                            var sw = System.Diagnostics.Stopwatch.StartNew();

                            // マスターコンテナ型.

                            var containerTypeName = string.Format("{0}+{1}", masterType.FullName, ContainerClassName);

                            var containerType = masterType.Assembly.GetType(containerTypeName);

                            // マスターレコード型.

                            var recordTypeName = string.Format("{0}+{1}", masterType.FullName, RecordClassName);

                            var recordType = masterType.Assembly.GetType(recordTypeName);

                            // マスター読み込み.

                            var master = await LoadMasterData(indexFilePath, containerType, recordType, config.DataFormat);

                            // MessagePackファイル作成.

                            var filePath = GetGenerateMasterFilePath(exportDirectory, masterFileName, fileNameCryptoKey);

                            var fileName = Path.GetFileNameWithoutExtension(filePath);

                            var versionHash = await GenerateMasterFile(filePath, master,  dataCryptoKey, lz4Compression);

                            // バージョンハッシュ.

                            lock (fileHashDictionary)
                            {
                                fileHashDictionary.Add(fileName, versionHash);
                            }

                            sw.Stop();

                            lock (logBuilder)
                            {
                                logBuilder.AppendFormat("{0} ({1:F2}ms)", masterName, sw.Elapsed.TotalMilliseconds).AppendLine();
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

                await Task.WhenAll(tasks);

                // バージョンファイル作成.

                var commitHash = await GetCommitHash();

                GenerateMasterVersionFile(exportDirectory, fileHashDictionary);

                logBuilder.Insert(0, string.Format("CommitHash : {0}\n\n", commitHash));

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

        private static async Task<object> LoadMasterData(string indexFilePath, Type containerType, Type recordType, SerializationFileUtility.Format format)
        {
            var masterDataDirectory = Directory.GetParent(indexFilePath);

            var recordFileDirectory = PathUtility.Combine(masterDataDirectory.FullName, RecordFolderName);

            if (!Directory.Exists(recordFileDirectory)) { return null; }

            // Load records.

            var records = await LoadAllRecords(recordFileDirectory, recordType, format);

            // Create MessagePack binary.

            var container = BuildMasterContainer(containerType, recordType, records);

            return container;
        }
        
        private static async Task<IDictionary<string, object>> LoadAllRecords(string recordFileDirectory, Type recordType, SerializationFileUtility.Format format)
        {
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

            return records;
        }

        private static object BuildMasterContainer(Type containerType, Type recordType, IDictionary<string, object> records)
        {
            // コンテナ作成.
            var container = Activator.CreateInstance(containerType);

            // レコード情報.

            var flags = BindingFlags.GetField | BindingFlags.SetField | BindingFlags.Public | BindingFlags.Instance;

            var fieldInfo = Reflection.GetFieldInfo(containerType, "records", flags);

            // 配列化.

            var array = records.Values.ToArray();

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

        private static async Task<string> GenerateMasterFile(string filePath, object master, AesCryptoKey dataCryptoKey, bool lz4Compression)
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

        public static async Task<string> GetCommitHash()
        {
            var masterConfig = MasterConfig.Instance;

            if (string.IsNullOrEmpty(masterConfig.SourceDirectory)){ return null; }

            var processExecute = new ProcessExecute("git", "log --pretty=%H -n 1")
            {
                WorkingDirectory = masterConfig.SourceDirectory,
            };

            var result = await processExecute.StartAsync();

            if (string.IsNullOrEmpty(result.Output)){ return null; }

            // 改行コードを削除.
            return result.Output.Replace("\r", "").Replace("\n", "");
        }

        private static void GenerateMasterVersionFile(string filePath, IDictionary<string, string> versionHashDictionary)
        {
            var builder = new StringBuilder();

            foreach (var item in versionHashDictionary)
            {
                builder.AppendFormat("{0},{1}", item.Key, item.Value).AppendLine();
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
