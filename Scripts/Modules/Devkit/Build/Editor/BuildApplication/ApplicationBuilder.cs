
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Extensions;
using Modules.Devkit.Prefs;

namespace Modules.Devkit.Build
{
    public interface IApplicationBuilder
    {
        BuildTarget BuildTarget { get; }

        BuildOptions BuildOptions { get; }

        bool Development { get; }

        IReadOnlyList<string> DefineSymbols { get; }

        void OnCreateInstance();

        /// <summary> 成果物ファイル名取得. </summary>
        string GetApplicationName();

        /// <summary> 成果物出力フォルダ名取得. </summary>
        string GetExportFolderName();

        /// <summary> ビルドに含めるシーンパス取得. </summary>
        string[] GetAllScenePaths();

        /// <summary> ビルド前処理. </summary>
        UniTask<bool> OnBeforeBuild();

        /// <summary> ビルド後処理. </summary>
        UniTask OnAfterBuild(bool isSuccess);

        /// <summary> ビルド成功時処理. </summary>
        UniTask OnBuildSuccess(BuildReport buildReport);

        /// <summary> ビルドエラー時処理. </summary>
        UniTask OnBuildError(BuildReport buildReport);
    }

    public abstract class ApplicationBuilder<TBuildParameter> : IApplicationBuilder where TBuildParameter : BuildParameter, new()
    {
        //----- params -----

        private static class Prefs
        {
            public static TBuildParameter savedParameter
            {
                get { return ProjectPrefs.Get<TBuildParameter>(typeof(Prefs).FullName + "-savedParameter", null); }
                set { ProjectPrefs.Set(typeof(Prefs).FullName + "-savedParameter", value); }
            }
        }

        //----- field -----

        protected TBuildParameter parameter = null;

        //----- property -----

        public BuildTarget BuildTarget { get { return parameter.buildTarget; } }

        public BuildOptions BuildOptions { get { return parameter.buildOptions; } }

        public bool Development { get { return parameter.development; } }

        public IReadOnlyList<string> DefineSymbols { get { return parameter.defineSymbols; } }

        //----- method -----

        public void SetBuildParameter(TBuildParameter buildParameter)
        {
            parameter = buildParameter;

            SaveParameter();
        }

        protected TBuildParameter LoadParameter()
        {
            if (Prefs.savedParameter == null) { return null; }
            
            parameter = Prefs.savedParameter;

            return parameter;
        }

        protected void SaveParameter()
        {
            if (parameter == null) { return; }
            
            Prefs.savedParameter = parameter;
        }

        public void OnCreateInstance()
        {
            LoadParameter();

            Debug.Log(parameter.ToJson());
        }

        public virtual string[] GetAllScenePaths()
        {
            var scenePaths = EditorBuildSettings.scenes
                .Where(scene => scene != null)
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenePaths.IsEmpty())
            {
                throw new Exception("Nothing scene to build.");
            }

            return scenePaths;
        }

        /// <summary> 成果物ファイル名取得. </summary>
        public virtual string GetApplicationName()
        {
            return UnityPathUtility.GetProjectName();
        }

        /// <summary> 成果物出力フォルダ名取得. </summary>
        public virtual string GetExportFolderName()
        {
            return string.Empty;
        }

        /// <summary> ビルド前処理. </summary>
        public virtual UniTask<bool> OnBeforeBuild()
        {
            return UniTask.FromResult(false);
        }

        /// <summary> ビルド後処理. </summary>
        public virtual UniTask OnAfterBuild(bool isSuccess)
        {
            return UniTask.CompletedTask;
        }

        /// <summary> ビルド成功時処理. </summary>
        public virtual UniTask OnBuildSuccess(BuildReport buildReport)
        {
            Debug.Log("Build success.");

            return UniTask.CompletedTask;
        }

        /// <summary> ビルドエラー時処理. </summary>
        public virtual UniTask OnBuildError(BuildReport buildReport)
        {
            Debug.LogError("Build failed.");

            return UniTask.CompletedTask;
        }
    }
}
