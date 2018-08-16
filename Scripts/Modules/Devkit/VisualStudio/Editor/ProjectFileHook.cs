
#if ENABLE_VSTU

using UnityEditor;
using VisualStudioToolsUnity;

namespace Modules.Devkit.VisualStudio
{
    [InitializeOnLoad]
    public static class ProjectFileHook
    {
        private const int csharpLangVersion = 4;

        static ProjectFileHook()
        {
            ProjectFilesGenerator.SolutionFile.AddHook(SolutionFile.AbortIfExists);

            ProjectFilesGenerator.ProjectFile.AddHook(ProjectFile.IsUnityOrEditorOrPluginsProject, ProjectFile.ExcludeAnotherLanguageReference);
            ProjectFilesGenerator.ProjectFile.AddHook(ProjectFile.IsUnityOrEditorOrPluginsProject, args => ProjectFile.SetCSharpLangVersion(args, csharpLangVersion));

            // Analyzer
            ProjectFilesGenerator.ProjectFile.AddHook(ProjectFile.IsUnityProject, args => ProjectFile.IncludeAnalyzer(args, "UniRxAnalyzer"));
            ProjectFilesGenerator.ProjectFile.AddHook(ProjectFile.IsUnityProject, args => ProjectFile.IncludeAnalyzer(args, "MessagePackAnalyzer"));
        }
    }
}

#endif
