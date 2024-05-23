
using System;
using UnityEngine;
using UnityEditor;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.Prefs;

namespace Modules.ExternalAssets
{
    public sealed class AssetInfoManifestAutoUpdater : Singleton<AssetInfoManifestAutoUpdater>
    {
        //----- params -----

        private const float UpdaterIntervalSeconds = 1f;

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

        //----- property -----

        public bool Enable
        {
            get { return Prefs.enable; }
            set
            {
                Prefs.enable = value;

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
                Instance.StartAutoUpdater();
            };
        }

        private void StartAutoUpdater()
        {
            if (!Enable){ return; }

            UpdaterMainLoop().Forget();
        }

        private async UniTask UpdaterMainLoop()
        {
            while (Enable)
            {
                var skip = false;

                // フォーカスがない.
                skip |= !UnityEditorInternal.InternalEditorUtility.isApplicationActive;

                // 実行中.
                skip |= EditorApplication.isPlayingOrWillChangePlaymode;

                // コンパイル中.
                skip |= EditorApplication.isCompiling;

                // アセットインポート中.
                skip |= AssetDatabase.IsAssetImportWorkerProcess();

                // 生成リクエストがあったら生成.
                if (!skip && Prefs.requestGenerate)
                {
                    await AssetInfoManifestGenerator.Generate();

                    Prefs.requestGenerate = false;
                }

                await UniTask.Delay(TimeSpan.FromSeconds(UpdaterIntervalSeconds), DelayType.Realtime);
            }
        }
    }
}