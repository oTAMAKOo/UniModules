
#if ENABLE_VSTU

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Modules.Devkit.VisualStudio
{
    partial class SolutionFile
    {
        /// <summary>
        /// ソリューション ファイル (.sln) が既に存在する場合は、
        /// 既存のソリューション ファイルをそのまま使用するためのフック処理を実行.
        /// </summary>
        public static void AbortIfExists(SolutionFileGenerationArgs args)
        {
            // 既に .sln がある場合は VSTU には触らせない.
            if (File.Exists(args.Filename))
            {
                args.Content = File.ReadAllText(args.Filename);
                args.Handled = true;
            }
        }
    }
}

#endif
