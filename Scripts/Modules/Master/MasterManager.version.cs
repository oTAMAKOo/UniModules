
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Console;

namespace Modules.Master
{
	public sealed partial class MasterManager
	{
		//----- params -----

		private const string VersionFileName = "version";

		private const char VersionSeparator = '|';

		//----- field -----

		// バージョン情報.
		private Dictionary<string, string> versions = null;

		// バージョンファイル難読化ハンドラー.
		private IVersionFileHandler versionFileHandler = null;

		// バージョン更新処理.
		private IDisposable updateVersionDisposable = null;

		// バージョン更新中.
		private bool versionSaveRunning = false;

		//----- property -----

		//----- method -----
		
		private void InitializeVersion()
		{
			versions = new Dictionary<string, string>();

			versionFileHandler = new DefaultVersionFileHandler();
		}

		public void SetVersionFileHandler(IVersionFileHandler versionFileHandler)
		{
			this.versionFileHandler = versionFileHandler;
		}

		private bool CheckVersion(IMaster master, string masterVersion)
		{
			var result = true;

			var fileName = GetMasterFileName(master.GetType());

			var filePath = GetFilePath(master);

			// ファイルがなかったらバージョン不一致.
			result &= File.Exists(filePath);

			// ローカル保存されているバージョンと一致するか.
			result &= versions[fileName] == masterVersion;

			return result;
		}

		private void UpdateVersion(IMaster master, string masterVersion)
		{
			var fileName = GetMasterFileName(master.GetType());

			versions[fileName] = masterVersion;

			if (updateVersionDisposable == null)
			{
				updateVersionDisposable = Observable.Timer(TimeSpan.FromSeconds(1f))
					.SelectMany(_ => SaveVersion().ToObservable())
					.Subscribe()
					.AddTo(Disposable);
			}
		}

		public async UniTask SaveVersion()
		{
			if (versionSaveRunning) { return; }

			var versionFilePath = PathUtility.Combine(InstallDirectory, VersionFileName);

			try
			{
				versionSaveRunning = true;

				await UniTask.SwitchToThreadPool();

				var builder = new StringBuilder();

				foreach (var version in versions)
				{
					builder.Append(version.Key);
					builder.Append(VersionSeparator);
					builder.Append(version.Value);
					builder.AppendLine();
				}

				var text = builder.ToString();

				while (true)
				{
					if (!FileUtility.IsFileLocked(versionFilePath)) { break; }

					await UniTask.NextFrame();
				}

				var bytes = Encoding.UTF8.GetBytes(text);

				if (versionFileHandler != null)
				{
					bytes = await versionFileHandler.Encode(bytes);
				}

				#if UNITY_2021_1_OR_NEWER

				await File.WriteAllBytesAsync(versionFilePath, bytes);

				#else

				File.WriteAllBytes(versionFilePath, bytes);

				#endif
			}
			catch (Exception e)
			{
				Debug.LogException(e);

				if (!string.IsNullOrEmpty(versionFilePath) && File.Exists(versionFilePath))
				{
					File.Delete(versionFilePath);
				}
			}
			finally
			{
				await UniTask.SwitchToMainThread();

				if (updateVersionDisposable != null)
				{
					updateVersionDisposable.Dispose();
					updateVersionDisposable = null;
				}

				versionSaveRunning = false;
			}
		}

		private async UniTask LoadVersion()
		{
			var logText = string.Empty;

			var versionFilePath = PathUtility.Combine(InstallDirectory, VersionFileName);

			try
			{
				await UniTask.SwitchToThreadPool();

				var sw = System.Diagnostics.Stopwatch.StartNew();

				versions.Clear();

				if (File.Exists(versionFilePath))
				{
					#if UNITY_2021_1_OR_NEWER

					var bytes = await File.ReadAllBytesAsync(versionFilePath);

					#else

					var bytes = File.ReadAllBytes(versionFilePath);

					#endif

					if (versionFileHandler != null)
					{
						bytes = await versionFileHandler.Decode(bytes);
					}

					var text = Encoding.UTF8.GetString(bytes);

					using (var sr = new StringReader(text))
					{
						while (-1 < sr.Peek())
						{
							var line = await sr.ReadLineAsync();

							var parts = line.Split(VersionSeparator);

							var fileName = parts.ElementAtOrDefault(0);
							var version = parts.ElementAtOrDefault(1);

							if (!string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(version))
							{
								versions[fileName] = version;
							}
						}
					}

					sw.Stop();

					logText = $"LoadVersion: ({sw.Elapsed.TotalMilliseconds:F2}ms)";
				}
			}
			catch
			{
				if (File.Exists(versionFilePath))
				{
					File.Delete(versionFilePath);
				}
			}
			finally
			{
				await UniTask.SwitchToMainThread();
			}

			if (!string.IsNullOrEmpty(logText))
			{
				UnityConsole.Event(ConsoleEventName, ConsoleEventColor, logText);
			}
		}

		public async UniTask ClearVersion(IMaster master)
		{
			var fileName = GetMasterFileName(master.GetType());

			if (versions.ContainsKey(fileName))
			{
				versions.Remove(fileName);
			}

			await SaveVersion();
		}

		private void DeleteVersionFile()
		{
			versions.Clear();

			try
			{
				var versionFilePath = PathUtility.Combine(InstallDirectory, VersionFileName);

				if (File.Exists(versionFilePath))
				{
					var cFileInfo = new FileInfo(versionFilePath);

					// 読み取り専用属性がある場合は、読み取り専用属性を解除.
					if ((cFileInfo.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					{
						cFileInfo.Attributes = FileAttributes.Normal;
					}

					File.Delete(versionFilePath);
				}
			}
			catch (Exception ex)
			{
				Debug.LogException(ex);
			}
		}
	}
}