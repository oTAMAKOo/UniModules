
using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.Console;
using Modules.Devkit.Prefs;
using Modules.Devkit.Project;
using UniRx;
using Amazon.ElasticLoadBalancingV2.Model;

namespace Modules.ExternalAssets
{
    public sealed class AssetInfoManifestAutoUpdater : Singleton<AssetInfoManifestAutoUpdater>
    {
        //----- params -----

        private const int UpdaterIntervalSeconds = 3;

        public static class Prefs
        {
            public static bool enable
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-enable", true); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-enable", value); }
            }

            public static bool requestGenerate
            {
                get { return ProjectPrefs.GetBool(typeof(Prefs).FullName + "-requestGenerate", false); }
                set { ProjectPrefs.SetBool(typeof(Prefs).FullName + "-requestGenerate", value); }
            }
        }

        //----- field -----

        private FileSystemWatcher watcher = null;

        private bool fileSystemChangedWatch = false;

        private string manifestPath = null;

        //----- property -----

        public bool Enable
        {
            get { return Prefs.enable; }
            set { Prefs.enable = value; }
        }

        //----- method -----

        private AssetInfoManifestAutoUpdater() { }

        [InitializeOnLoadMethod]
        public static void InitializeOnLoadMethod()
        {
            if (Application.isBatchMode){ return; }

            EditorApplication.delayCall += () =>
            {
                Instance.Initialize();
            };
        }

        private void Initialize()
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            if (projectResourceFolders == null) { return; }

            void OnEnableChanged()
            {
                if (Enable)
                {
                    StartAutoUpdater();
                }
                else
                {
                    StopAutoUpdater();
                }
            }

            this.ObserveEveryValueChanged(x => x.Enable)
                .Subscribe(_ => OnEnableChanged())
                .AddTo(Disposable);

            var externalAssetPath = projectResourceFolders.ExternalAssetPath;

            var manifestAssetPath = AssetInfoManifestGenerator.GetManifestPath(externalAssetPath);

            manifestPath = UnityPathUtility.ConvertAssetPathToFullPath(manifestAssetPath);

            if (Enable)
            {
                StartAutoUpdater();
            }
        }

        private void StartAutoUpdater()
        {
            var projectResourceFolders = ProjectResourceFolders.Instance;

            if (projectResourceFolders == null) { return; }

            var rootFolderPath = UnityPathUtility.ConvertAssetPathToFullPath(projectResourceFolders.ExternalAssetPath);

            watcher = new FileSystemWatcher(rootFolderPath, "*.*");

            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;

            // サブディレクトリ監視.
            watcher.IncludeSubdirectories = true;

            // 監視を開始.
            watcher.EnableRaisingEvents = true;

            //イベント設定.
            watcher.Created += FileSystemUpdated;
            watcher.Deleted += FileSystemUpdated;
            watcher.Renamed += FileSystemUpdated;

            // 更新ループ実行.
            UpdaterMainLoop().Forget();
        }

        private void StopAutoUpdater()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher = null;
            }

            Enable = false;
        }

        private void FileSystemUpdated(object sender, FileSystemEventArgs e)
        {
            var path = PathUtility.ConvertPathSeparator(e.FullPath);

            if (path.StartsWith(manifestPath)){ return; }

            EditorApplication.delayCall += () =>
            {
                fileSystemChangedWatch = true;
            };
        }

        private async UniTask UpdaterMainLoop()
        {
            while (Enable)
            {
                // 既にリクエスト済み.
                var skip = Prefs.requestGenerate;

                // 実行中.
                skip |= EditorApplication.isPlayingOrWillChangePlaymode;

                // コンパイル中.
                skip |= EditorApplication.isCompiling;

                // アセットインポート中.
                skip |= AssetDatabase.IsAssetImportWorkerProcess();

                // 更新があったら生成.
                if (!skip)
                {
                    // ファイルシステムの変更を検知していた場合.
                    if (fileSystemChangedWatch)
                    {
                        Prefs.requestGenerate = true;

                        fileSystemChangedWatch = false;
                    }

                    if (Prefs.requestGenerate)
                    {
                        AssetInfoManifestGenerator.Generate();

                        UnityConsole.Info("Auto update AssetInfoManifest");

                        Prefs.requestGenerate = false;
                    }
                }

                await UniTask.Delay(TimeSpan.FromSeconds(UpdaterIntervalSeconds));
            }
        }
    }
}