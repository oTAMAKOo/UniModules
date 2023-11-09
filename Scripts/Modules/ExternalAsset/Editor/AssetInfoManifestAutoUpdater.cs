
using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;
using Modules.Devkit.Prefs;
using Modules.Devkit.Project;

namespace Modules.ExternalAssets
{
    public sealed class AssetInfoManifestAutoUpdater : Singleton<AssetInfoManifestAutoUpdater>
    {
        //----- params -----

        private const float UpdaterIntervalSeconds = 0.5f;

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
            set
            {
                Prefs.enable = value;

                StopAutoUpdater();

                if (value)
                {
                    StartAutoUpdater();
                }
            }
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
            if (watcher == null){ return; }

            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
            watcher = null;
        }

        private void FileSystemUpdated(object sender, FileSystemEventArgs e)
        {
            var extension = Path.GetExtension(e.FullPath);

            if (extension == ".meta"){ return; }

            if (PathUtility.IsFolder(e.FullPath)) { return; }

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

                // フォーカスがある.
                skip |= UnityEditorInternal.InternalEditorUtility.isApplicationActive;

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
                        await AssetInfoManifestGenerator.Generate();

                        Prefs.requestGenerate = false;
                    }
                }

                await UniTask.Delay(TimeSpan.FromSeconds(UpdaterIntervalSeconds));
            }

            StopAutoUpdater();
        }
    }
}