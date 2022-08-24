
#if ENABLE_VSTU

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Modules.Devkit.VisualStudio
{
    /// <summary>
    /// VSTU によるプロジェクト ファイル (.csproj) 作成処理をフックする機能を提供.
    /// </summary>
    public partial class ProjectFile : FileGeneration<ProjectFileGenerationArgs>
    {
        internal ProjectFile()
        {
            #if UNITY_2018_2_OR_NEWER

            VisualStudioFileCallback.AddGeneratedCSProjectCallback(HandleProjectFileGeneration);

            #else

            SyntaxTree.VisualStudio.Unity.Bridge.ProjectFilesGenerator.ProjectFileGeneration = HandleProjectFileGeneration;

            #endif
        }

        private string HandleProjectFileGeneration(string filename, string content)
        {
            if (string.IsNullOrEmpty(filename)) return content;

            var args = new ProjectFileGenerationArgs(filename, content);

            foreach (var handler in this.Handlers.Where(x => x.NameSelector(filename)).Select(x => x.Handler))
            {
                handler(args);
                if (args.Handled || args.Cancel){ break; }
            }

            if (args.Cancel) return content;

            var writer = new Utf8StringWriter();
            args.Content.Save(writer);

            return writer.ToString();
        }
    }
}

#endif
