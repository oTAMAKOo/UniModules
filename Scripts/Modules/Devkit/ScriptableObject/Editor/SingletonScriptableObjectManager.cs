
using System;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UniRx;
using Extensions;

namespace Modules.Devkit.ScriptableObjects
{
    public abstract class ReloadableScriptableObject : ScriptableObject
    {
        private Subject<Unit> onReload = null;

        protected void OnReload()
        {
            if (onReload != null)
            {
                onReload.OnNext(Unit.Default);
            }
        }

        public IObservable<Unit> OnReloadAsObservable()
        {
            return onReload ?? (onReload = new Subject<Unit>());
        }

        public abstract void Reload();
    }

    public sealed class ReloadableScriptableObjectManager: Singleton<ReloadableScriptableObjectManager>
    {
        //----- params -----

        //----- field -----

        private FileSystemWatcher watcher = null;

        private Dictionary<string, ReloadableScriptableObject> targets = null;

        private HashSet<ReloadableScriptableObject> reloadTargets = null;

        //----- property -----

        //----- method -----

        private ReloadableScriptableObjectManager(){ }

        protected override void OnCreate()
        {
            targets = new Dictionary<string, ReloadableScriptableObject>();
            reloadTargets = new HashSet<ReloadableScriptableObject>();

            var projectFolderPath = UnityPathUtility.GetProjectFolderPath();

            watcher = new FileSystemWatcher(projectFolderPath, "*.asset");

            watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite;

            // サブディレクトリ監視.
            watcher.IncludeSubdirectories = true;

            // 監視を開始.
            watcher.EnableRaisingEvents = true;

            //イベント設定.
            watcher.Changed += FileSystemUpdated;
            watcher.Renamed += FileSystemUpdated;

            // 更新ループ実行.
            WatcherLoop().Forget();
        }

        public void Register(ReloadableScriptableObject target)
        {
            var assetPath = AssetDatabase.GetAssetPath(target);

            var path = UnityPathUtility.ConvertAssetPathToFullPath(assetPath);

            targets[path] = target;
        }

        private void FileSystemUpdated(object sender, FileSystemEventArgs e)
        {
            var path = PathUtility.ConvertPathSeparator(e.FullPath);

            var target = targets.GetValueOrDefault(path);

            if (target == null){ return; }

            reloadTargets.Add(target);
        }

        private async UniTask WatcherLoop()
        {
            while (true)
            {
                var skip = false;

                // 実行中.
                skip |= EditorApplication.isPlayingOrWillChangePlaymode;

                // コンパイル中.
                skip |= EditorApplication.isCompiling;

                // アセットインポート中.
                skip |= AssetDatabase.IsAssetImportWorkerProcess();

                // 更新があったら生成.
                if (reloadTargets.Any() && !skip)
                {
                    foreach (var reloadTarget in reloadTargets)
                    {
                        reloadTarget.Reload();
                    }

                    reloadTargets.Clear();
                }

                await UniTask.NextFrame();
            }
        }
    }
}