
#if ENABLE_VSTU

using System;
using System.Collections.Generic;
using System.Linq;

namespace Modules.Devkit.VisualStudio
{
    /// <summary>
    /// Visual Studio Tools for Unity をラップし、.sln または .csproj ファイル作成処理をフックするための機能を提供.
    /// </summary>
    public static class ProjectFilesGenerator
    {
        /// <summary>
        /// VSTU によるソリューション ファイル (.sln) 作成処理のフック.
        /// </summary>
        public static SolutionFile SolutionFile { get; private set; }

        /// <summary>
        /// VSTU によるプロジェクト ファイル (.csproj) 作成処理のフック.
        /// </summary>
        public static ProjectFile ProjectFile { get; private set; }

        static ProjectFilesGenerator()
        {
            SolutionFile = new SolutionFile();
            ProjectFile = new ProjectFile();
        }
    }
}

#endif
