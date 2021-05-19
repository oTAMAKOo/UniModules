
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
        Task OnBeforeBuild();

        /// <summary> ビルド後処理. </summary>
        Task OnAfterBuild(bool isSuccess);

        /// <summary> ビルド成功時処理. </summary>
        Task OnBuildSuccess(BuildReport buildReport);

        /// <summary> ビルドエラー時処理. </summary>
        Task OnBuildError(BuildReport buildReport);
    }

    public abstract class ApplicationBuilder<TBuildParameter> : IApplicationBuilder where TBuildParameter : BuildParameter, new()
    {
        //----- params -----

        private static class Prefs
        {
            public static TBuildParameter savedParameter
            {
                get { return ProjectPrefs.Get<TBuildParameter>("ApplicationBuilderPrefs-savedParameter", null); }
                set { ProjectPrefs.Set("ApplicationBuilderPrefs-savedParameter", value); }
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
        public virtual Task OnBeforeBuild()
        {
            return Task.CompletedTask;
        }

        /// <summary> ビルド後処理. </summary>
        public virtual Task OnAfterBuild(bool isSuccess)
        {
            return Task.CompletedTask;
        }

        /// <summary> ビルド成功時処理. </summary>
        public virtual Task OnBuildSuccess(BuildReport buildReport)
        {
            Debug.Log("Build success.");

            return Task.CompletedTask;
        }

        /// <summary> ビルドエラー時処理. </summary>
        public virtual Task OnBuildError(BuildReport buildReport)
        {
            Debug.LogError("Build failed.");

            return Task.CompletedTask;
        }
    }
}
