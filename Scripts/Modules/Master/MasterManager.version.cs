
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
            // ローカル保存されているバージョンと一致するか.

            var fileName = GetMasterFileName(master.GetType());

            var localVersion = versions.GetValueOrDefault(fileName);

            if (localVersion != masterVersion) { return false; }

            // ファイルがなかったらバージョン不一致.

            var filePath = GetFilePath(master);

            if(!File.Exists(filePath)){ return false; }

            return true;
        }

        private void UpdateVersion(IMaster master, string masterVersion)
        {
            var fileName = GetMasterFileName(master.GetType());

            lock (versions)
            {
                versions[fileName] = masterVersion;
            }

            if (updateVersionDisposable == null)
            {
                updateVersionDisposable = Observable.Timer(TimeSpan.FromSeconds(1f))
                    .Subscribe(_ => SaveVersion().Forget())
                    .AddTo(Disposable);
            }
        }

        public async UniTask SaveVersion()
        {
            if (versionSaveRunning) { return; }

            if (InstallDirectory.StartsWith(UnityPathUtility.StreamingAssetsPath)){ return; }

            var versionFilePath = PathUtility.Combine(InstallDirectory, VersionFileName);

            try
            {
                versionSaveRunning = true;

                await UniTask.RunOnThreadPool(async () =>
                {
                    var builder = new StringBuilder();
                
                    lock (versions)
                    {
                        foreach (var version in versions)
                        {
                            builder.Append(version.Key);
                            builder.Append(VersionSeparator);
                            builder.Append(version.Value);
                            builder.AppendLine();
                        }
                    }

                    var text = builder.ToString();

                    var bytes = Encoding.UTF8.GetBytes(text);

                    if (versionFileHandler != null)
                    {
                        bytes = versionFileHandler.Encode(bytes);
                    }

                    using (var fs = new FileStream(versionFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true))
                    {
                        await fs.WriteAsync(bytes, 0, (int)bytes.Length).ConfigureAwait(false);
                    }
                });
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
                if (updateVersionDisposable != null)
                {
                    updateVersionDisposable.Dispose();
                    updateVersionDisposable = null;
                }

                versionSaveRunning = false;
            }
        }

        public async UniTask LoadVersion()
        {
            var logText = string.Empty;

            var versionFilePath = PathUtility.Combine(InstallDirectory, VersionFileName);

            try
            {
                await UniTask.RunOnThreadPool(async () =>
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();

                    versions.Clear();

                    if (File.Exists(versionFilePath))
                    {
                        byte[] bytes = null;

                        using (var fs = new FileStream(versionFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                        {
                            bytes = new byte[fs.Length];

                            await fs.ReadAsync(bytes, 0, (int)fs.Length).ConfigureAwait(false);
                        }

                        if (versionFileHandler != null)
                        {
                            bytes = versionFileHandler.Decode(bytes);
                        }

                        var text = Encoding.UTF8.GetString(bytes);

                        using (var sr = new StringReader(text))
                        {
                            while (-1 < sr.Peek())
                            {
                                var line = await sr.ReadLineAsync().ConfigureAwait(false);

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
                });
            }
            catch
            {
                if (File.Exists(versionFilePath))
                {
                    File.Delete(versionFilePath);
                }
            }

            if (!string.IsNullOrEmpty(logText))
            {
                UnityConsole.Event(ConsoleEventName, ConsoleEventColor, logText);
            }
        }

        public async UniTask ClearVersion(IMaster master)
        {
            var fileName = GetMasterFileName(master.GetType());

            lock (versions)
            {
                if (versions.ContainsKey(fileName))
                {
                    versions.Remove(fileName);
                }
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