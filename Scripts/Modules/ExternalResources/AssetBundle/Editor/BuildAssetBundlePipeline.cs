
using UnityEngine.Build.Pipeline;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Pipeline;
using UnityEditor.Build.Pipeline.Interfaces;
using System.Collections.Generic;

using BuildCompression = UnityEngine.BuildCompression;

namespace Modules.AssetBundles.Editor
{
    public interface IBuildAssetBundlePipeline
    {
        BuildTarget BuildTarget { get; }

        BuildResult Build(string outputPath);

        BuildResult Build(string outputPath, AssetBundleBuild[] buildMap);
    }

    public sealed class BuildResult
    {
        public ReturnCode ExitCode { get; }

        public bool IsSuccess { get { return ExitCode == ReturnCode.Success; } }

        public IBundleBuildResults BundleBuildResults { get; }

        public BuildResult(ReturnCode exitCode, IBundleBuildResults bundleBuildResults)
        {
            ExitCode = exitCode;
            BundleBuildResults = bundleBuildResults;
        }

        public BundleDetails? GetDetails(string assetbundleName)
        {
            if (!BundleBuildResults.BundleInfos.ContainsKey(assetbundleName)){ return null; }

            return BundleBuildResults.BundleInfos[assetbundleName];
        }
    }

    public sealed class BuildAssetBundlePipeline : IBuildAssetBundlePipeline
    {
        //----- params -----

        //----- field -----

        //----- property -----

        public BuildTarget BuildTarget { get { return EditorUserBuildSettings.activeBuildTarget; } }

        //----- method -----

        /// <summary> 全アセットバンドルをビルド </summary>
        public BuildResult Build(string outputPath)
        {
            var buildMap = ContentBuildInterface.GenerateAssetBundleBuilds();

            return Build(outputPath, buildMap);
        }
        
        /// <summary> 指定されたアセットバンドルをビルド </summary>
        public BuildResult Build(string outputPath, AssetBundleBuild[] buildMap)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(BuildTarget);

            var buildParams = new BundleBuildParameters(BuildTarget, buildTargetGroup, outputPath)
            {
                BundleCompression = BuildCompression.LZ4,
            };

            var buildContent = new BundleBuildContent(buildMap);

            var tasks = new List<IBuildTask>();
            
            tasks.AddRange(DefaultBuildTasks.Create(DefaultBuildTasks.Preset.AssetBundleBuiltInShaderExtraction));
            
            var exitCode = ContentPipeline.BuildAssetBundles(buildParams, buildContent, out var results, tasks);
            
            return new BuildResult(exitCode, results);
        }
    }
}