
#if ENABLE_VSTU

using UnityEditor;
using VisualStudioToolsUnity;

namespace Modules.Devkit.VisualStudio
{
    [InitializeOnLoad]
    public static class ProjectFileHook
    {
        #if CSHARP_7_OR_LATER

        private const int csharpLangVersion = 7;

        #else

        private const int csharpLangVersion = 4;

        #endif

        static ProjectFileHook()
        {
            ProjectFilesGenerator.SolutionFile.AddHook(SolutionFile.AbortIfExists);

            ProjectFilesGenerator.ProjectFile.AddHook(ProjectFile.IsUnityOrEditorOrPluginsProject, ProjectFile.ExcludeAnotherLanguageReference);
            ProjectFilesGenerator.ProjectFile.AddHook(ProjectFile.IsUnityOrEditorOrPluginsProject, args => ProjectFile.SetCSharpLangVersion(args, csharpLangVersion));

            // Analyzer
            ProjectFilesGenerator.ProjectFile.AddHook(ProjectFile.IsUnityProject, args => ProjectFile.IncludeAnalyzer(args, "UniRxAnalyzer"));
        }
    }
}

#endif
