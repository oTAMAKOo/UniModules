
#if ENABLE_VSTU

using System.Linq;

namespace Modules.Devkit.VisualStudio
{
    /// <summary>
    /// VSTU によるソリューション ファイル (.sln) 作成処理をフックする機能を提供.
    /// </summary>
    public partial class SolutionFile : FileGeneration<SolutionFileGenerationArgs>
    {
        internal SolutionFile()
        {
            #if UNITY_2018_2_OR_NEWER

            VisualStudioFileCallback.AddGeneratedSlnSolutionCallback(HandleSolutionFileGeneration);

            #else

            SyntaxTree.VisualStudio.Unity.Bridge.ProjectFilesGenerator.SolutionFileGeneration = HandleSolutionFileGeneration;

            #endif
        }

        private string HandleSolutionFileGeneration(string filename, string content)
        {
            if (string.IsNullOrEmpty(filename)) return content;

            var args = new SolutionFileGenerationArgs(filename, content);

            foreach (var handler in this.Handlers.Where(x => x.NameSelector(filename)).Select(x => x.Handler))
            {
                handler(args);

				if (args.Handled || args.Cancel) { break; }
            }

            return args.Cancel ? content : args.Content;
        }
    }
}

#endif
