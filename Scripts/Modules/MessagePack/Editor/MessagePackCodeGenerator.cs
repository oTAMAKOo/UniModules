
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Extensions;

namespace Modules.MessagePack
{
    public static class MessagePackCodeGenerator
    {
        //----- params -----

        //----- field -----

        //----- property -----

        //----- method -----

        public static bool Generate()
        {
            var generateInfo = new MessagePackCodeGenerateInfo();

            var csFilePath = generateInfo.CsFilePath;

            var csFileHash = GetCsFileHash(csFilePath);

            var processExecute = CreateMpcProcess(generateInfo);

            var codeGenerateResult = processExecute.Start();
            
            var isSuccess = codeGenerateResult.ExitCode == 0;

            OutputGenerateLog(isSuccess, csFilePath, processExecute);

            if (isSuccess)
            {
                ImportGeneratedCsFile(csFilePath, csFileHash);
            }
            else
            {
                using (new DisableStackTraceScope())
                {
                    Debug.LogError(codeGenerateResult.Output);
                }
            }

            return isSuccess;
        }

        private static ProcessExecute CreateMpcProcess(MessagePackCodeGenerateInfo generateInfo)
        {
            var messagePackConfig = MessagePackConfig.Instance;

            var mpcPath = messagePackConfig.MpcPath;

            var command = string.Empty;
            var argument = string.Empty;
            
            if (string.IsNullOrEmpty(messagePackConfig.ProcessCommand))
            {
                command = mpcPath;
                argument = generateInfo.MpcArgument;
            }
            else
            {
                command = messagePackConfig.ProcessCommand;
                argument = $" {mpcPath}{generateInfo.MpcArgument}";

                #if UNITY_EDITOR_OSX

                command = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), command);

                #endif
            }

            if (string.IsNullOrEmpty(command) || string.IsNullOrEmpty(argument))
            {
                throw new InvalidOperationException("command or argument is null.");
            }

            var processExecute = new ProcessExecute(command, argument)
            {
                Encoding = Encoding.UTF8,
            };

            return processExecute;
        }

        private static void ImportGeneratedCsFile(string csFilePath, string csFileHash)
        {
            var messagePackConfig = MessagePackConfig.Instance;

            var assetPath = UnityPathUtility.ConvertFullPathToAssetPath(csFilePath);
            
            // 名前空間定義 global::xxxxxxが定義されない問題対応.

            var forceAddGlobalSymbols = messagePackConfig.ForceAddGlobalSymbols;

            if (forceAddGlobalSymbols != null && forceAddGlobalSymbols.Any())
            {
                var builder = new StringBuilder();

                var encode = new UTF8Encoding(true);

                using (var sr = new StreamReader(csFilePath, encode))
                {
                    var code = sr.ReadToEnd();

                    builder.AppendLine("// ForceAddGlobalSymbols by MessagePackCodeGenerator.cs.");

                    foreach (var forceAddGlobalSymbol in forceAddGlobalSymbols)
                    {
                        builder.AppendLine(forceAddGlobalSymbol);
                    }

                    builder.AppendLine();
                    builder.Append(code);
                }

                using (var sw = new StreamWriter(csFilePath, false, encode))
                {
                    sw.Write(builder.ToString());
                }
            }

            // 差分があったらインポート.

            var hash = GetCsFileHash(csFilePath);

            if (File.Exists(csFilePath))
            {
                if (csFileHash != hash)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                }
            }
        }

        private static string GetCsFileHash(string csFilePath)
        {
            var hash = string.Empty;

            if (!File.Exists(csFilePath)){ return string.Empty; }

            using (var fs = new FileStream(csFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var md5 = new MD5CryptoServiceProvider();

                var bs = md5.ComputeHash(fs);

                md5.Clear();

                hash = BitConverter.ToString(bs).ToLower().Replace("-", "");
            }

            return hash;
        }

        private static void OutputGenerateLog(bool result, string csFilePath, ProcessExecute processExecute)
        {
            using (new DisableStackTraceScope())
            {
                var logBuilder = new StringBuilder();

                logBuilder.AppendLine();
                logBuilder.AppendLine();
                logBuilder.AppendFormat("MessagePack file : {0}", csFilePath).AppendLine();
                logBuilder.AppendLine();
                logBuilder.AppendFormat("Execute:").AppendLine();
                logBuilder.AppendLine($"{processExecute.Command} {processExecute.Arguments}");

                if (result)
                {
                    logBuilder.Insert(0, "MessagePack code generate success!");

                    Debug.Log(logBuilder.ToString());
                }
                else
                {
                    logBuilder.Insert(0, "MessagePack code generate failed.");

                    Debug.LogError(logBuilder.ToString());
                }
            }
        }
    }
}
