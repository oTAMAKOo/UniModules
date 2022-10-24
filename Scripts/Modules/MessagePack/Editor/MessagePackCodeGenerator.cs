
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Cysharp.Threading.Tasks;
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

        public static async UniTask<bool> GenerateAsync()
        {
            var generateInfo = new MessagePackCodeGenerateInfo();

            var csFilePath = generateInfo.CsFilePath;

            var csFileHash = GetCsFileHash(csFilePath);

            var processExecute = CreateMpcProcess(generateInfo);

            var isSuccess = false;

            try
            {
                var result = await processExecute.StartAsync().AsUniTask();

                isSuccess = result.ExitCode == 0;

                OutputGenerateLog(isSuccess, csFilePath, processExecute);

                if (isSuccess)
                {
                    ImportGeneratedCsFile(csFilePath, csFileHash);
                }
                else
                {
                    using (new DisableStackTraceScope())
                    {
                        var message = result.Error;

                        if (string.IsNullOrEmpty(message))
                        {
                            message = result.Output;
                        }

                        Debug.LogError(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                /* Canceled */
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return isSuccess;
        }

        private static ProcessExecute CreateMpcProcess(MessagePackCodeGenerateInfo generateInfo)
        {
            var messagePackConfig = MessagePackConfig.Instance;

            var command = messagePackConfig.MpcRelativePath;
            var argument = generateInfo.MpcArgument;

            if (string.IsNullOrEmpty(command))
            {
	            command = GetMpcCommand();

				#if UNITY_EDITOR_OSX

				if (command.EndsWith("dotnet"))
				{
					argument = "mpc " + argument;
				}

				#endif
            }

            var processExecute = new ProcessExecute(command, argument)
            {
                Encoding = Encoding.GetEncoding("Shift_JIS"),
            };

            return processExecute;
        }

		private static string GetMpcCommand()
		{
			var result = "mpc";

			#if UNITY_EDITOR_WIN

			// 環境変数.
			var variable = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Process);

			if (variable != null)
			{
				foreach (var item in variable.Split(';'))
				{
					var path = PathUtility.Combine(item, "mpc.exe");

					if (!File.Exists(path)){ continue; }

					result = path;

					break;
				}
			}
			
			#endif

			#if UNITY_EDITOR_OSX

			var mpcPathCandidate = new string[]
			{
				"$HOME/.dotnet/tools/",
				"/usr/local/bin/",
			};

			foreach (var item in mpcPathCandidate)
			{
				var path = PathUtility.Combine(item, "dotnet");

				if (!File.Exists(path)){ continue; }

				result = path;

				break;
			}

			#endif

			return result;
		}

		private static void ImportGeneratedCsFile(string csFilePath, string csFileHash)
        {
            var assetPath = UnityPathUtility.ConvertFullPathToAssetPath(csFilePath);
            
            // global::UnityEngineが定義されない問題対応.
            
            var builder = new StringBuilder();

            var encode = new UTF8Encoding(true);

            using (var sr = new StreamReader(csFilePath, encode))
            {
                var code = sr.ReadToEnd();

                builder.AppendLine("// Fix : Missing global::UnityEngine error.");
                builder.AppendLine("using UnityEngine;");
                builder.AppendLine();
                builder.Append(code);
            }

            using (var sw = new StreamWriter(csFilePath, false, encode))
            {
                sw.Write(builder.ToString());
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
